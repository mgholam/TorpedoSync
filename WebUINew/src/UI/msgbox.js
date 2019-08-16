import msgbox from './Msgbox.svelte'

// var color = "#999";

// function setColor(col) {
//     color = col;
// }

function Ok(message = "", title = "") {
    var c = createmb();
    c.Ok(message, title);
    c.show = true;
}

function OkCancel(message = "", title = "", okfunc) {
    var c = createmb();
    c.OkCancel(message, title, okfunc);
    c.show = true;
}

function YesNo(message = "", title = "", okfunc) {
    var c = createmb();
    c.YesNo(message, title, okfunc);
    c.show = true;
}

function InputLine(initial = "", title = "", placeholder = "Enter a string", okfunc) {
    var c = createmb();
    c.Input(initial, title, placeholder, false, okfunc);
    c.show = true;
}

function InputBox(initial = "", title = "", placeholder = "Enter a string", okfunc) {
    var c = createmb();
    c.Input(initial, title, placeholder, true, okfunc);
    c.show = true;
}

let _last = null;

function Progress(message = "", title = "") {
    var c = createmb();
    _last = c;
    c.Progress(message, title);
    c.show = true;
}

// close spinner
function Close() {
    if (_last) {
        _last.show = false;
        _last = null;
    }
}

function createmb() {
    var n = document.createElement("div")
    var e = document.body.appendChild(n);
    // console.log(color);
    let m = new msgbox({ target: n, props: { node: n } });
    return m;
}

export default { Ok, OkCancel, YesNo, InputLine, InputBox, Progress, Close /*, setColor*/ };