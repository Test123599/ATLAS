// Small, explicit storage bridge used by the server-side Blazor circuit.
// The JWT pair stays in localStorage so a reload can restore the session;
// a marker + role hint cookie lets HTTP-level [Authorize] attributes avoid
// redirecting an already signed-in user to the login page on direct navigation.
// The API remains the sole authority for every data access.
window.atlasStorage = {
    get: function (key) {
        try { return window.localStorage.getItem(key) || null; } catch (e) { return null; }
    },
    set: function (key, value) {
        try {
            window.localStorage.setItem(key, value);
            if (key === 'atlas.access') window.atlasStorage.syncRouteCookies(value);
        } catch (e) { /* storage disabled */ }
    },
    setPair: function (accessKey, refreshKey, access, refresh) {
        try {
            window.localStorage.setItem(accessKey, access);
            window.localStorage.setItem(refreshKey, refresh);
            window.atlasStorage.syncRouteCookies(access);
        } catch (e) { /* storage disabled */ }
    },
    remove: function (key) {
        try {
            window.localStorage.removeItem(key);
            if (key === 'atlas.access') {
                document.cookie = 'atlas_auth=; path=/; max-age=0; SameSite=Lax';
                document.cookie = 'atlas_roles=; path=/; max-age=0; SameSite=Lax';
            }
        } catch (e) { /* storage disabled */ }
    },
    removePair: function (accessKey, refreshKey) {
        try {
            window.localStorage.removeItem(accessKey);
            window.localStorage.removeItem(refreshKey);
            document.cookie = 'atlas_auth=; path=/; max-age=0; SameSite=Lax';
            document.cookie = 'atlas_roles=; path=/; max-age=0; SameSite=Lax';
        } catch (e) { /* storage disabled */ }
    },
    syncRouteCookies: function (token) {
        try {
            var roles = [];
            var part = String(token || '').split('.')[1];
            if (part) {
                part = part.replace(/-/g, '+').replace(/_/g, '/');
                while (part.length % 4) part += '=';
                var bytes = Uint8Array.from(atob(part), function (c) { return c.charCodeAt(0); });
                var payload = JSON.parse(new TextDecoder().decode(bytes));
                Object.keys(payload).forEach(function (key) {
                    if (key.indexOf('role') < 0) return;
                    var value = payload[key];
                    if (Array.isArray(value)) roles = roles.concat(value);
                    else if (value) roles.push(String(value));
                });
            }
            document.cookie = 'atlas_auth=1; path=/; SameSite=Lax';
            document.cookie = 'atlas_roles=' + encodeURIComponent(roles.join(',')) + '; path=/; SameSite=Lax';
        } catch (e) { /* malformed token: the API will reject it anyway */ }
    }
};
