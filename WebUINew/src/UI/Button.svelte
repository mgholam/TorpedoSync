<script>
  export let type = "button";
  export let href = null;
  export let mode = null;
  export let color = null;
  export let disabled = false;
</script>

<style>
  button {
    min-width: 100px;
    font-size: medium;
    background: var(--theme-color);
    background-image: linear-gradient(
      to bottom,
      var(--theme-color),
      var(--theme-darker)
    );
    border-radius: 15px;
    color: var(--theme-white);
    padding: 10px;
    border: solid var(--theme-color) 1px;
    margin-bottom: 5px;
    transition: all .1s;
  }

  button:hover {
    background-image: linear-gradient(
      to bottom,
      var(--theme-lighter),
      var(--theme-darker)
    );
    box-shadow: 5px 5px 10px rgba(0, 0, 0, 0.5);
    transform : translateY(-1px);
  }
  /* button,
  a {
    font: inherit;
    font-size: large;
    border: 1px solid #cf0056;
    background: #cf0056;
    padding: 15px;
    color: white;
    border-radius: 15px;
    box-shadow: 1px 1px 5px rgba(0, 0, 0, 0.26);
    cursor: pointer;
    text-decoration: none;
    min-width: 100px;
  } */

  /* button:focus {
    outline: none;
  } */

  /* button:hover,
  button:active,
  a:hover,
  a:active {
    background: #e40763;
    border-color: #e40763;
    box-shadow: 1px 1px 18px rgba(77, 51, 51, 0.26);
  } */

  button:disabled,
  button:disabled:hover,
  button:disabled:active {
    background: #ccc;
    border-color: #ccc;
    color: #959595;
    box-shadow: none;
    cursor: not-allowed;
  }

  .success {
    background: #01a129;
    border-color: #01a129;
  }

  .success:hover,
  .success:active {
    background: #1ac745;
    border-color: #1ac745;
  }

  .outline {
    background: transparent;
    color: #cf0056;
    box-shadow: none;
  }

  .outline:hover,
  .outline:active {
    background: #ffc7de;
    box-shadow: none;
  }

  .outline:disabled,
  .outline:disabled:hover,
  .outline:disabled:active {
    background: transparent;
    color: #ccc;
  }

  .outline.success {
    border-color: #01a129;
    color: #01a129;
  }

  .outline.success:hover,
  .outline.success:active {
    background: #c2ffd1;
  }
</style>

{#if href}
  <a {href}>
    <slot />
  </a>
{:else}
  <button class={mode} {color} {type} on:click {disabled}>
    <slot />
  </button>
{/if}
