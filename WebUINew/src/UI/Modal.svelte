<script>
  import { createEventDispatcher, onMount } from "svelte";
  import { fly, fade, scale } from "svelte/transition";
  import Button from "./Button.svelte";

  export let title;
  export let minheight = 180;
  export let minwidth = 200;
  export let show = false;

  onMount(() => {
    modaldiv.style.height = minheight;
    modaldiv.style.width = minwidth;
  });

  const dispatch = createEventDispatcher();

  function closeModal() {
    dispatch("cancel");
  }

  let modaldiv;

  let md = false;
  let mi = null;
  let allowmmove = false;

  function mdown(e) {
    if (allowmmove) {
      md = true;
      // kludge : first time modaldiv values are 0
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
    if (h >= minheight) modaldiv.style.height = h + "px";
    e.stopPropagation();
    e.cancelBubble = true;
  }

  function stopDrag(e) {
    document.documentElement.removeEventListener("mousemove", doDrag, false);
    document.documentElement.removeEventListener("mouseup", stopDrag, false);
  }
</script>

<style>
  :global(.header) {
    background-color: var(--theme-light);
    border: solid 2px var(--theme-darker);
    margin-top: 5px;
    padding: 5px;
    font-family: Tahoma;
    /* font-size: medium; */
  }

  :global(.header input) {
    font-size: medium;
    padding: 3px;
    width: 98%;
    font-family: Tahoma;
  }

  :global(.header label) {
    margin-right: 3px;
    text-align: right;
    /*width: 150px;*/
    display: inline-block;
  }

  :global(.header table) {
    width: 100%;
  }

  :global(.header table tr td:first-child) {
    width: 150px;
    text-align: end;
  }

  :global(input[type="checkbox"]) {
    width: 30px !important;
    height: 30px;
    zoom: 1.2;
    /*** FIREFOX FIX ***/
    -moz-transform: scale(2);
  }
  
  .modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100vh;
    background: rgba(0, 0, 0, 0.55);
    z-index: 49;
  }

  .modal {
    position: absolute;
    top: 50%;
    left: 50%;
    background-color: #eee;
    border-radius: 15px;
    z-index: 50;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.96);
    transform: translate(-50%, -50%);
    overflow: hidden;
  }

  h1 {
    background-color: var(--theme-color);
    color: var(--theme-darker);
    font-size: 1rem;
    padding: 0.5rem;
    margin: 0;
    border-top-left-radius: 15px;
    border-top-right-radius: 15px;
    border: 1px solid var(--theme-darker);
    font-family: Tahoma;
    cursor: grab;
    text-align: center;
  }

  .content {
    padding: 1rem;
  }

  footer {
    position: fixed;
    padding: 1rem 0;
    width: 100%;
    text-align: center;
    bottom: 0;
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
</style>

{#if show}
  <div transition:fade class="modal-backdrop" />
  <div
    transition:scale={{ y: 300, duration: 200 }}
    class="modal"
    on:mousedown={mdown}
    bind:this={modaldiv}>
    <h1 on:mousedown={() => allowmmove = true}>{title}</h1>
    <div class="content">
      <slot name="content" />
    </div>
    <footer>
      <slot name="footer">
        <Button on:click={closeModal}>Close</Button>
      </slot>
    </footer>
    <div class="resizer" on:mousedown={rdown} />
  </div>
{/if}
