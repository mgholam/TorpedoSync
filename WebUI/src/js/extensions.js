// -----------------------------------  jquery like ----------------------------------------------
$ = function(selector) {
    if (!(this instanceof $))
        return new $(selector);
    var doc = document;
    this.nodes = doc.getElementById(selector);
    if (this.nodes == null)
        this.nodes = doc.querySelectorAll(selector);
    else
        return this.nodes;
};

$.prototype = {
    each: function(callback) {
        if (this.nodes != null) {
            for (var i = 0; i < this.nodes.length; ++i) {
                callback.call(this.nodes[i], i);
            }
        } else
            callback.call(this, 0);
        return this; // to allow chaining like jQuery does
    },
    hide: function() {
        this.each(function(i) { this.style.display = "none"; })
        return this;
    },
    show: function() {
        this.each(function(i) { this.style.display = ""; })
        return this;
    },
    first: function() {
        if (this.nodes != null)
            return this.nodes[0];
    }
};

$.get = function(url, callback, error) {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url);

    xhr.onreadystatechange = function() {
        if (xhr.readyState === 1) { return; } else if (xhr.readyState === 4) {
            if (xhr.status < 400) {
                data = null;
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
                error(xhr);
        }
    };
    xhr.onerror = function() {
        $.showDialog("Sorry", "<p>Connection Failed!</p>");
    };
    xhr.withCredentials = false;
    xhr.send();
};

$.get2 = function(url, callback,error){
    return $.get(window.ServerURL+url, callback,error);
}

$.post = function(url, data, ret) {
    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = function() {
        if (xhr.readyState === 4) { //if complete
            if (ret !== undefined)
                ret(xhr.responseText);
        }
    };
    xhr.open('POST', url);
    xhr.withCredentials = false;
    // xhr.setRequestHeader('Access-Control-Allow-Headers', '*');
    // xhr.setRequestHeader('Access-Control-Allow-Origin', '*');
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify(data));
};

$.post2 = function(url,data, ret){
    return $.post(window.ServerURL+url, data, ret);
}


$.closemodal = function () {
    var d = $("openModal");
    if (d !== null) {
        document.body.removeChild(d);
        document.onkeypress = null;
        document.location = "#";
        $.showingdialog = false;
        window.location.reload(true);
    }
};

$.showingdialog = false;

$.showDialog = function (caption, innerhtml) {
    var dd = $("openModal");
    if (dd["nodes"] === undefined)
        return;
    $.showingdialog = true;
    var body = document.body;
    var d = document.createElement('div');
    d.id = "openModal";
    d.className = "modalDialog";
    d.innerHTML = '<div><a onclick="$.closemodal(); event.preventDefault();" title="Close" class="close">X</a><h2>' + caption + '</h2>' + innerhtml + '</div>';
    body.appendChild(d);
    document.location = "#openModal";
    document.onkeypress = function () {
        $.closemodal();
        window.location.reload(true);
    };
};  



$.findBy = function(list, search) {
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


$.filter = function(list, filter, sortcol, order) {
    var result = $.findBy(list, filter);
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

//------------------------------------------------------------------------------------------------------------------------
//----------------------------------------------- extensions -------------------------------------------------------------
(function() {
    $.murmur = function(str) {
        return murmurhash2_32_gc(str, 0xc58f1a7b);
    }

    function murmurhash2_32_gc(str, seed) {
        var
            l = str.length,
            h = seed ^ l,
            i = 0,
            k;

        while (l >= 4) {
            k =
                ((str.charCodeAt(i) & 0xff)) |
                ((str.charCodeAt(++i) & 0xff) << 8) |
                ((str.charCodeAt(++i) & 0xff) << 16) |
                ((str.charCodeAt(++i) & 0xff) << 24);

            k = (((k & 0xffff) * 0x5bd1e995) + ((((k >>> 16) * 0x5bd1e995) & 0xffff) << 16));
            k ^= k >>> 24;
            k = (((k & 0xffff) * 0x5bd1e995) + ((((k >>> 16) * 0x5bd1e995) & 0xffff) << 16));

            h = (((h & 0xffff) * 0x5bd1e995) + ((((h >>> 16) * 0x5bd1e995) & 0xffff) << 16)) ^ k;

            l -= 4;
            ++i;
        }

        switch (l) {
            case 3:
                h ^= (str.charCodeAt(i + 2) & 0xff) << 16;
            case 2:
                h ^= (str.charCodeAt(i + 1) & 0xff) << 8;
            case 1:
                h ^= (str.charCodeAt(i) & 0xff);
                h = (((h & 0xffff) * 0x5bd1e995) + ((((h >>> 16) * 0x5bd1e995) & 0xffff) << 16));
        }

        h ^= h >>> 13;
        h = (((h & 0xffff) * 0x5bd1e995) + ((((h >>> 16) * 0x5bd1e995) & 0xffff) << 16));
        h ^= h >>> 15;

        return h >>> 0;
    }
}());

(function() {
    // Private array of chars to use
    var CHARS = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'.split('');
    $.uuid = function(len, radix) {
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
    $.uuidFast = function() {
        var chars = CHARS,
            uuid = new Array(36),
            rnd = 0,
            r;
        for (var i = 0; i < 36; i++) {
            if (i == 8 || i == 13 || i == 18 || i == 23) {
                uuid[i] = '-';
            } else if (i == 14) {
                uuid[i] = '4';
            } else {
                if (rnd <= 0x02) rnd = 0x2000000 + (Math.random() * 0x1000000) | 0;
                r = rnd & 0xf;
                rnd = rnd >> 4;
                uuid[i] = chars[(i == 19) ? (r & 0x3) | 0x8 : r];
            }
        }
        return uuid.join('');
    };

    // A more compact, but less performant, RFC4122v4 solution:
    $.uuidCompact = function() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            var r = Math.random() * 16 | 0,
                v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    };
}());