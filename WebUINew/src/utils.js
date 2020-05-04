function LOAD(url) {
  // @ts-ignore
  return fetch(window.ServerURL + url).then((r) => r.text());
}

function GET(url) {
  // @ts-ignore
  return fetch(window.ServerURL + url).then((r) => {
    if (r.ok) return r.json();
    else {
      return r.text().then((x) => {
        return Promise.reject({
          status: r.status,
          statusText: x,
        });
      });
    }
  });
}

function POST(url, data) {
  // @ts-ignore
  return fetch(window.ServerURL + url, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

function FINDBY(list, search) {
  return list.filter(function (item) {
    var r = false;
    var s = search.toLowerCase();
    for (var k in item) {
      if (k == "id" || k == "guid") continue;
      if (item[k] == null) continue;
      if (("" + item[k]).toLowerCase().indexOf(s) > -1) {
        r = true;
        break;
      }
    }
    return r;
  });
}

function FILTER(list, filter, sortcol, order) {
  var result = FINDBY(list, filter);
  var ascDesc = order ? 1 : -1;
  if (sortcol)
    return result.sort((a, b) => {
      if (typeof a[sortcol] == "number") return ascDesc * (a[sortcol] - b[sortcol]);
      else return ascDesc * ("" + a[sortcol]).localeCompare("" + b[sortcol]);
    });
  else return result;
}

// Private array of chars to use
var CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".split("");
function uuid(len, radix) {
  var chars = CHARS,
    uuid = [],
    i;
  radix = radix || chars.length;

  if (len) {
    // Compact form
    for (i = 0; i < len; i++) uuid[i] = chars[0 | (Math.random() * radix)];
  } else {
    // rfc4122, version 4 form
    var r;

    // rfc4122 requires these characters
    uuid[8] = uuid[13] = uuid[18] = uuid[23] = "-";
    uuid[14] = "4";

    // Fill in random data.  At i==19 set the high bits of clock sequence as
    // per rfc4122, sec. 4.1.5
    for (i = 0; i < 36; i++) {
      if (!uuid[i]) {
        r = 0 | (Math.random() * 16);
        uuid[i] = chars[i == 19 ? (r & 0x3) | 0x8 : r];
      }
    }
  }

  return uuid.join("");
}

export default {
  LOAD,
  GET,
  POST,
  FINDBY,
  FILTER,
  uuid,
};
