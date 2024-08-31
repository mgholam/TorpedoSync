<script>
  // import { createEventDispatcher } from "svelte";
  import { fly, fade, scale } from "svelte/transition";
  import Button from "./Button.svelte";

  export let node = "";
  export let show = false;
  // export let color = "#999";

  // $: if (color != "#999") {
  //   // console.log(color);
  //   document.documentElement.style.setProperty(
  //     "--theme-darker",
  //     // LightenDarkenColor(color, -66)
  //     hexToHSL(color, -0.3)
  //   );
  //   document.documentElement.style.setProperty(
  //     "--theme-dark",
  //     // LightenDarkenColor(color, -33)
  //     hexToHSL(color, -0.1)
  //   );
  //   document.documentElement.style.setProperty("--theme-color", color);
  //   document.documentElement.style.setProperty(
  //     "--theme-light",
  //     // LightenDarkenColor(color, 33)
  //     hexToHSL(color, 0.1)
  //   );
  //   document.documentElement.style.setProperty(
  //     "--theme-lighter",
  //     // LightenDarkenColor(color, 66)
  //     hexToHSL(color, 0.3)
  //   );
  // } else {
  //   document.documentElement.style.setProperty("--theme-darker", "#666");
  //   document.documentElement.style.setProperty("--theme-dark", "#777");
  //   document.documentElement.style.setProperty("--theme-color", "#999");
  //   document.documentElement.style.setProperty("--theme-light", "#ccc");
  //   document.documentElement.style.setProperty("--theme-lighter", "#eee");
  // }

  let Msg = "";
  let Title = "";
  let showok = false;
  let okf = null;
  let txtclose = "Cancel";
  let txtok = "OK";
  let style = "";

  let progressmode = false;
  let inputmode = false;
  let inputline;
  let inpval = "";
  let inpplaceholder = "";
  let multline = false;

  let modaldiv;
  let md = false;
  let allowmmove = false;
  let mi = null;

  let minheight = 180;
  let minwidth = 300;

  export function Ok(message, title) {
    Msg = message;
    Title = title;
    style = "background-color: white;";
    txtclose = "OK";
  }

  export function OkCancel(message, title, okfunc) {
    Msg = message;
    Title = title;
    showok = true;
    okf = okfunc;
  }

  export function YesNo(message, title, okfunc) {
    Msg = message;
    Title = title;
    showok = true;
    txtclose = "No";
    txtok = "Yes";
    okf = okfunc;
  }

  export function Input(initial, title, placeholder, multiline, okfunc) {
    inputmode = true;
    inpval = initial;
    inpplaceholder = placeholder;
    multline = multiline;
    Title = title;
    showok = true;
    minheight = 346;
    okf = okfunc;
  }

  export function Progress(message, title) {
    progressmode = true;
    Msg = message;
    Title = title;
    style = "background-color: white;";
    txtclose = "OK";
  }

  function chkclose() {
    allowmmove = false;
    md = false;
    //if (inputmode == false) close();
  }

  function close() {
    show = false;
    setTimeout(() => {
      document.body.removeChild(node);
    }, 1000);
  }

  function okclick() {
    if (okf) {
      if (inputmode) {
        if (inpval) {
          okf(inpval);
          close();
          return;
        } else return;
      }
      okf();
    }

    close();
  }

  function mdown(e) {
    if (allowmmove) {
      md = true;
      var r = modaldiv.getBoundingClientRect();
      mi = {
        X: e.pageX - (r.left + r.width / 2),
        Y: e.pageY - (r.top + r.height / 2)
      };
      document.documentElement.addEventListener("mousemove", mmove, false);
      document.documentElement.addEventListener("mouseup", mup, false);
    }
  }

  function mup(e) {
    allowmmove = false;
    md = false;
    document.documentElement.removeEventListener("mousemove", mmove, false);
    document.documentElement.removeEventListener("mouseup", mup, false);
  }

  function mmove(e) {
    if (md && allowmmove) {
      modaldiv.style.left = e.pageX - mi.X;
      modaldiv.style.top = e.pageY - mi.Y;
    }
  }

  // resizer code
  var startX, startY, startWidth, startHeight;

  function rdown(e) {
    startX = e.clientX;
    startY = e.clientY;
    var m = document.defaultView.getComputedStyle(modaldiv);

    startWidth = parseInt(m.width, 10);
    startHeight = parseInt(m.height, 10);
    document.documentElement.addEventListener("mousemove", doDrag, false);
    document.documentElement.addEventListener("mouseup", stopDrag, false);
  }

  function doDrag(e) {
    var h = startHeight + e.clientY - startY;
    var w = startWidth + e.clientX - startX;
    if (w >= minwidth) modaldiv.style.width = w + "px";
    // if (h >= minheight) modaldiv.style.height = h + "px";
    e.stopPropagation();
    e.cancelBubble = true;
  }

  function stopDrag(e) {
    document.documentElement.removeEventListener("mousemove", doDrag, false);
    document.documentElement.removeEventListener("mouseup", stopDrag, false);
  }

  // function hexToHSL(H, alpha) {
  //   // Convert hex to RGB first
  //   let r = 0,
  //     g = 0,
  //     b = 0;
  //   if (H.length == 4) {
  //     r = "0x" + H[1] + H[1];
  //     g = "0x" + H[2] + H[2];
  //     b = "0x" + H[3] + H[3];
  //   } else if (H.length == 7) {
  //     r = "0x" + H[1] + H[2];
  //     g = "0x" + H[3] + H[4];
  //     b = "0x" + H[5] + H[6];
  //   }
  //   // Then to HSL
  //   r /= 255;
  //   g /= 255;
  //   b /= 255;
  //   let cmin = Math.min(r, g, b),
  //     cmax = Math.max(r, g, b),
  //     delta = cmax - cmin,
  //     h = 0,
  //     s = 0,
  //     l = 0;

  //   if (delta == 0) h = 0;
  //   else if (cmax == r) h = ((g - b) / delta) % 6;
  //   else if (cmax == g) h = (b - r) / delta + 2;
  //   else h = (r - g) / delta + 4;

  //   h = Math.round(h * 60);

  //   if (h < 0) h += 360;

  //   l = (cmax + cmin) / 2;
  //   s = delta == 0 ? 0 : delta / (1 - Math.abs(2 * l - 1));
  //   s = +(s * 100).toFixed(1);
  //   l += alpha;
  //   l = +(l * 100).toFixed(1);

  //   return "hsl(" + h + "," + s + "%," + l + "%)";
  // }

  // function LightenDarkenColor(col, amt) {
  //   var usePound = false;

  //   if (col[0] == "#") {
  //     col = col.slice(1);
  //     usePound = true;
  //   }

  //   amt = 1 + amt / 100;

  //   var num = parseInt(col, 16);

  //   var r = (num >> 16) * amt;

  //   if (r > 255) r = 255;
  //   else if (r < 0) r = 0;

  //   var b = ((num >> 8) & 0x00ff) * amt;

  //   if (b > 255) b = 255;
  //   else if (b < 0) b = 0;

  //   var g = (num & 0x0000ff) * amt;

  //   if (g > 255) g = 255;
  //   else if (g < 0) g = 0;

  //   return (usePound ? "#" : "") + (g | (b << 8) | (r << 16)).toString(16);
  // }
</script>

<style>
  /* :global(:root) {
    --theme-darker: #666;
    --theme-dark: #777;
    --theme-color: #999;
    --theme-light: #ccc;
    --theme-lighter: #eee;
    --theme-white: #fff;
  } */
  .txt {
    width: 100%;
    resize: none;
    font-size: large;
    padding: 10px;
    border: 1px solid var(--theme-light);
    border-radius: 5px;
    border-style: solid;
    background-color: var(--theme-white);
  }
  .modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100vh;
    background: rgba(0, 0, 0, 0.55);
    z-index: auto;
  }

  .modal {
    position: absolute;
    top: 50%;
    left: 50%;
    min-width: 30vw;
    padding: 10px;
    /* background-color: var(--theme-lighter); */
    background-color: #eee;
    border-radius: 15px;
    z-index: 1000;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.96);
    transform: translate(-50%, -50%);
  }

  .ok {
    margin-right: 20px;
    display: inline;
  }

  .msg {
    font-size: x-large;
  }
  h1 {
    text-align: center;
    margin: 0;
    border-bottom: 1px solid var(--theme-light);
    cursor: grab;
  }

  .content {
    text-align: center;
    padding: 1rem;
  }

  footer {
    align-content: center;
    text-align: center;
    font-size: large;
    padding: 1rem;

    /* position: absolute;
    width : 90%;
    bottom: 0; */
  }

  .resizer {
    width: 15px;
    height: 15px;
    background-color: var(--theme-dark);
    position: absolute;
    cursor: se-resize;
    right: 0;
    bottom: 0;
  }

  button {
    min-width: 100px;
    font-size: large;
    background: var(--theme-color);
    background-image: linear-gradient(
      to bottom,
      var(--theme-color),
      var(--theme-darker)
    );
    border-radius: 15px;
    color: var(--theme-white);
    padding: 15px;
    border: solid var(--theme-color) 1px;
    margin-bottom: 5px;
    transition: all 0.1s;
  }

  button:hover {
    background-image: linear-gradient(
      to bottom,
      var(--theme-lighter),
      var(--theme-darker)
    );
    box-shadow: 5px 5px 10px rgba(0, 0, 0, 0.5);
    transform: translateY(-1px);
  }

  .outline {
    background: var(--theme-white);
    /* color: var(--theme-darker); */
    color: #666;
  }
  .outline:hover {
    background-image: linear-gradient(
      to bottom,
      var(--theme-white),
      var(--theme-light)
    );
    /* color: var(--theme-white); */
    box-shadow: none;
    transform: none;
  }

  .spinner {
    position: relative;
    text-align: center;
    display: block;
    border: 0.5rem solid var(--theme-lighter); /* Light grey */
    border-top: 0.5rem solid var(--theme-dark); /* Blue  #3498db*/
    border-radius: 50%;
    width: 80px;
    height: 80px;
    animation: spin 0.8s linear infinite;
    margin: auto;
    margin-top: 20px;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }
</style>

<svelte:options accessors={true} />

{#if show}
  <div transition:fade class="modal-backdrop" />
  <div
    transition:scale={{ y: 300, duration: 200 }}
    class="modal"
    {style}
    on:mousedown={mdown}
    bind:this={modaldiv}>
    <h1 on:mousedown={() => (allowmmove = true)}>{Title}</h1>
    <div class="content">
      {#if inputmode}
        {#if multline}
          <textarea
            class="txt"
            rows="10"
            placeholder={inpplaceholder}
            bind:value={inpval} />
        {:else}
          <input class="txt" placeholder={inpplaceholder} bind:value={inpval} />
        {/if}
      {:else}
        <label class="msg">{Msg}</label>
        {#if progressmode}
          <label class="spinner">&nbsp;</label>
        {/if}
      {/if}
    </div>
    {#if !progressmode}
      <footer>
        <div>
          <slot name="footer">
            {#if showok}
              <div class="ok">
                <button on:click={okclick}>{txtok}</button>
              </div>
            {/if}
            <button class="outline" on:click={close}>{txtclose}</button>
          </slot>
        </div>
      </footer>
      {#if inputmode}
        <div class="resizer" on:mousedown={rdown} />
      {/if}
    {/if}
  </div>
{/if}
