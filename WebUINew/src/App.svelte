<script>
  import { tick, onMount } from "svelte";
  // import NavPanel from "./UI/nav.svelte";
  import Button from "./UI/Button.svelte";
  import Fa from "svelte-fa";
  import icons from "./icons.js";

  import HelpPage from "./pages/help.svelte";
  import Share from "./pages/share.svelte";
  import Connection from "./pages/connection.svelte";
  import Logs from "./pages/logs.svelte";
  import Settings from "./pages/settings.svelte";

  let activetabid = "";
  let tabs = [
    // {
    //   title: "query",
    //   id: "aa",
    //   obj: null,
    //   active : false,
    // },
  ];
  let showmsg = false;
  let title = "";
  let modalyes = false;
  let server = "";

  onMount(() => {
    addtab({ name: "share", description: "Shares", icon: icons.faFolderOpen });
    addtab({
      name: "connection",
      description: "Connections",
      icon: icons.faExchangeAlt
    });
    addtab({ name: "settings", description: "Settings", icon: icons.faCog });
    addtab({ name: "logs", description: "Logs", icon: icons.faList });
    addtab({ name: "help", description: "Help", icon: icons.faQuestionCircle });
    tick().then(() => changetab(tabs[0].id));
    setTimeout(() => {
      // kludge to get the server
      server = window.ServerURL;
      // console.log("server : " + server);
    }, 1000);
  });

  function createcomponent(tabname, id, args) {
    var tab = null;
    switch (tabname) {
      case "help":
        tab = new HelpPage({
          target: document.getElementById(id)
        });
        break;
      case "share":
        tab = new Share({
          target: document.getElementById(id)
        });
        break;
      case "connection":
        tab = new Connection({
          target: document.getElementById(id)
        });
        break;
      case "logs":
        tab = new Logs({
          target: document.getElementById(id)
        });
        break;
      case "settings":
        tab = new Settings({
          target: document.getElementById(id)
        });
        break;
      // case "docview":
      //   tab = new DocView({
      //     target: document.getElementById(id),
      //     props: { docid: args ? args : null }
      //   });
      //   tab.$on("showrevs", event => {
      //     addtab({ name: "dochistory", description: "History" }, event.detail);
      //   });
      //   break;
    }

    return tab;
  }

  function addtab(nav, args) {
    var e = document.getElementById("mainid");
    // create div
    var divtest = document.createElement("div");
    var id = "id" + Math.floor(Math.random() * 1000);
    divtest.id = id;
    divtest.className = "container active";
    e.appendChild(divtest);

    activetabid = id;
    tick().then(() => {
      // create svelte component and bind to dom
      var tab = createcomponent(nav.name, id, args);
      // add to list
      tabs = [
        ...tabs,
        {
          title: nav.description,
          id: id,
          active: true,
          obj: tab,
          icon: nav.icon
        }
      ];
      changetab(id);
    });
  }

  function removetab(id) {
    // console.log(tabs);
    var t = tabs.find(x => x.id === id);
    if (t !== null && t.obj !== null) {
      // remove svelte component
      t.obj.$destroy();
      // remove from list
      tabs = tabs.filter(x => x.id !== id);
      // remove div from dom
      var e = document.querySelector("#mainid #" + id);
      e.remove();
    }
  }

  function showtab(nav) {
    // create content component
    // create new tab
    // bind content to tab
    addtab(nav);
  }

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
  :global(*) :global(*::after)/* :global(*::before) */ {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }
  :global(body) {
    box-sizing: border-box;
    font-family: Tahoma;
    color: #222;
    background: #414141;
  }
  :global(:root) {
    --theme-black: black;
    --theme-darker: #333;
    --theme-dark: #666;
    --theme-color: #999;
    --theme-light: #ccc;
    --theme-lighter: #eee;
    --theme-white: white;
    /*
    #222
    #414141
    #606060
    gray
    #3cb0fd
    red
    */
  }

  :global(.tab-content) {
    background-color: var(--theme-white);
    padding: 5px;
    padding-top: 15px;
    min-height: 90vh;
  }

  /* :global(.gi) {
    position: relative;
    top: 1px;
    display: inline-block;
    font-family: "Glyphicons Halflings";
    font-style: normal;
    font-weight: normal;
    line-height: 1;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  } */

  .Container {
    width: 100%;
    backface-visibility: hidden;
    will-change: overflow;
  }

  /* .Left, */
  .Middle
  /* .Right  */ {
    /* overflow: auto; */
    height: auto;
    -webkit-overflow-scrolling: touch;
    -ms-overflow-style: none;
  }

  /* .Left::-webkit-scrollbar, */
  .Middle::-webkit-scrollbar
  /* .Right::-webkit-scrollbar  */ {
    display: none;
  }

  /* .Left {
    width: 220px;
    float: left;
  } */

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

  /* .tab-links div #close {
    margin-left: 10px;
    padding: 2px;
  }

  .tab-links div #close:hover {
    background: red;
    color: white;
    text-align: center;
    -webkit-border-radius: 15px;
    -moz-border-radius: 15px;
    border-radius: 15px;
    -moz-box-shadow: 1px 1px 3px #000;
    -webkit-box-shadow: 1px 1px 3px #000;
    box-shadow: 1px 1px 3px #000;
  } */

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
  /* .closeall {
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
  } */
  .brand {
    color: var(--theme-white);
    font-size: x-large;
    float: left;
    padding: 4px;
    margin-right: 5px;
  }
</style>

<label style="float:right;color:gray;font-size:small">{server}</label>
<label class="brand">Torpedo Sync</label>
<div class="Container">
  <!-- <div class="Left">
    <NavPanel on:navclick={event => showtab(event.detail)} />
  </div> -->

  <div class="Middle">
    <ul class="tab-links">
      <!-- <span class="closeall" on:click={closeall} title="Close all tabs">X</span> -->
      {#each tabs as tab (tab.id)}
        <li
          id={tab.id}
          class:active={tab.id === activetabid}
          on:click={() => changetab(tab.id)}>
          <div>
            <label>
              <Fa icon={tab.icon} />
              <!-- <span class={tab.icon} /> -->
              {tab.title}
            </label>
            <!-- {#if tab.title !== 'Help'}
              <label id="close" on:click={() => closetab(tab.id)}>X</label>
            {/if} -->
          </div>
        </li>
      {/each}
    </ul>

    <div id="mainid" />

  </div>
</div>
