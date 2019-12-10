<script>
  import Fa from "svelte-fa";
  import icons from "../icons.js";

  export let mainid;
  export let tabs;
  export let activetabid = "";
  export let showCloseAll = false;

  $: changetab(activetabid);

  function removetab(id) {
    // console.log(tabs);
    var t = tabs.find(x => x.id === id);
    if (t !== null && t.obj !== null) {
      // remove svelte component
      t.obj.$destroy();
      // remove from list
      tabs = tabs.filter(x => x.id !== id);
      // remove div from dom
      var e = document.querySelector("#" + mainid + " #" + id);
      e.remove();
    }
  }

  // function showtab(nav) {
  //   // create content component
  //   // create new tab
  //   // bind content to tab
  //   addtab(nav);
  // }

  function changetab(id) {
    activetabid = id;
    tabs.forEach(x => {
      x.active = x.id == id ? true : false;
      x.obj.active = x.active;
    });
    tabs = tabs;
  }

  function closetab(id) {
    var i = tabs.findIndex(x => x.id === id);
    i--;
    changetab(tabs[i].id);
    removetab(id);
  }

  function closealltabs() {
    showmsg = false;

    tabs.forEach(x => {
      if (x.title !== "Help") closetab(x.id);
    });
  }

  function closeall() {
    showmsg = true;
    modalyes = false;

    if (tabs.length == 1) title = "No Tabs to close";
    else {
      title = "Do you want to close all the tabs?";
      modalyes = true;
    }
  }
</script>

<style>
  .tab-links:after {
    display: block;
    clear: both;
    content: "";
  }

  .tab-links li {
    margin: 0px 1px;
    float: left;
    list-style: none;
  }

  .tab-links {
    -webkit-padding-start: 5px;
    border-radius: 5px 5px 0px 0px;
    margin-bottom: 0px;
    margin-top: 10px;
  }

  .tab-links div {
    padding: 9px 15px;
    display: inline-block;
    border-radius: 5px 5px 0px 0px;
    background: var(--theme-darker);
    font-size: 16px;
    font-weight: 600;
    color: var(--theme-color);
    transition: all linear 0.15s;
    cursor: pointer;
  }

  .tab-links div:hover {
    background: var(--theme-light);
    color: var(--theme-darker);
    text-decoration: none;
  }

  li.active div,
  li.active {
    background: var(--theme-white) !important;
    border-radius: 5px 5px 0px 0px;
    border: 1px;
    color: var(--theme-darker);
  }
  .closeall {
    margin-left: -35px;
    color: white;
    padding: 8px;
    float: right !important;
    font-weight: bolder;
    text-align: center;
  }
  .closeall:hover {
    background: red;
    -webkit-border-radius: 15px;
    -moz-border-radius: 15px;
    border-radius: 15px;
  }
</style>

<ul class="tab-links">
  {#if showCloseAll}
    <span class="closeall" on:click={closeall} title="Close all tabs">X</span>
  {/if}
  {#each tabs as tab (tab.id)}
    <li
      id={tab.id}
      class:active={tab.id === activetabid}
      on:click={() => changetab(tab.id)}>
      <div>
        <label>
          <Fa icon={tab.icon} />
          {tab.title}
        </label>
      </div>
    </li>
  {/each}
</ul>
