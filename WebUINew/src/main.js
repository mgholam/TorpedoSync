import App from './App.svelte';

const app = new App({
    target: document.body,
});
window.ServerURL = document.location.protocol + "//" + document.location.hostname + ":" + document.location.port + "/";

window.GET = function(url, callback, error) {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', window.ServerURL + url);

    xhr.onreadystatechange = function() {
        if (xhr.readyState === 1) { return; } else if (xhr.readyState === 4) {
            if (xhr.status < 400) {
                var data = null;
                if (xhr.responseText !== "")
                    data = JSON.parse(xhr.responseText);
                else
                    data = "";
                if (callback)
                    callback(data);
            } else if (xhr.status === 401) {
                xhr.abort();
                if (callback)
                    callback("");
            } else if (error !== null)
                error(xhr.responseText);
        }
    };
    xhr.onerror = function(err) {
        // $.showDialog("Sorry", "<p>Connection Failed!</p>");
        console.log(err);
    };
    xhr.withCredentials = false;
    xhr.send();
};

window.POST = function(url, data, ret) {
    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = function() {
        if (xhr.readyState === 4) { //if complete
            if (ret !== undefined)
                ret(xhr.responseText);
        }
    };
    xhr.withCredentials = false;
    xhr.open('POST', window.ServerURL + url);
    // xhr.setRequestHeader('Access-Control-Allow-Headers', '*');
    xhr.setRequestHeader('Access-Control-Allow-Origin', '*');
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify(data));
};

window.LOAD = function(url, callback, error) {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', window.ServerURL + url);

    xhr.onreadystatechange = function() {
        if (xhr.readyState === 1) { return; } else if (xhr.readyState === 4) {
            // console.log(xhr);
            if (xhr.status < 400) {
                var data = null;
                if (xhr.responseText !== "")
                    data = xhr.responseText;
                else
                    data = "";
                if (callback)
                    callback(data);
            } else if (xhr.status === 401) {
                xhr.abort();
                if (callback)
                    callback("");
            } else if (error !== null)
                error(xhr.responseText);
        }
    };
    xhr.onerror = function(err) {
        // $.showDialog("Sorry", "<p>Connection Failed!</p>");
        console.log(err);
    };
    xhr.withCredentials = false;
    xhr.send();
};

window.FINDBY = function(list, search) {
    return list.filter(function(item) {
        var r = false;
        for (var k in item) {
            if (k === 'id' || k === 'guid')
                continue;
            if (item[k] === null)
                continue;
            if (("" + item[k]).toLowerCase().indexOf(search.toLowerCase()) > -1) {
                r = true;
                break;
            }
        }
        return r;
    });
};

window.FILTER = function(list, filter, sortcol, order) {
    var result = window.FINDBY(list, filter);
    var ascDesc = order ? 1 : -1;
    if (sortcol !== '')
        return result.sort((a, b) => {
            if (typeof a[sortcol] === "number")
                return ascDesc * (a[sortcol] - b[sortcol]);
            else
                return ascDesc * ("" + a[sortcol]).localeCompare("" + b[sortcol]);
        });
    else
        return result;
};

(function() {
    // Private array of chars to use
    var CHARS = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'.split('');
    window.uuid = function(len, radix) {
        var chars = CHARS,
            uuid = [],
            i;
        radix = radix || chars.length;

        if (len) {
            // Compact form
            for (i = 0; i < len; i++) uuid[i] = chars[0 | Math.random() * radix];
        } else {
            // rfc4122, version 4 form
            var r;

            // rfc4122 requires these characters
            uuid[8] = uuid[13] = uuid[18] = uuid[23] = '-';
            uuid[14] = '4';

            // Fill in random data.  At i==19 set the high bits of clock sequence as
            // per rfc4122, sec. 4.1.5
            for (i = 0; i < 36; i++) {
                if (!uuid[i]) {
                    r = 0 | Math.random() * 16;
                    uuid[i] = chars[(i == 19) ? (r & 0x3) | 0x8 : r];
                }
            }
        }

        return uuid.join('');
    };

    // A more performant, but slightly bulkier, RFC4122v4 solution.  We boost performance
    // by minimizing calls to random()
    // $.uuidFast = function() {
    //     var chars = CHARS,
    //         uuid = new Array(36),
    //         rnd = 0,
    //         r;
    //     for (var i = 0; i < 36; i++) {
    //         if (i == 8 || i == 13 || i == 18 || i == 23) {
    //             uuid[i] = '-';
    //         } else if (i == 14) {
    //             uuid[i] = '4';
    //         } else {
    //             if (rnd <= 0x02) rnd = 0x2000000 + (Math.random() * 0x1000000) | 0;
    //             r = rnd & 0xf;
    //             rnd = rnd >> 4;
    //             uuid[i] = chars[(i == 19) ? (r & 0x3) | 0x8 : r];
    //         }
    //     }
    //     return uuid.join('');
    // };

    // A more compact, but less performant, RFC4122v4 solution:
    // $.uuidCompact = function() {
    //     return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    //         var r = Math.random() * 16 | 0,
    //             v = c == 'x' ? r : (r & 0x3 | 0x8);
    //         return v.toString(16);
    //     });
    // };
}());

export default app;