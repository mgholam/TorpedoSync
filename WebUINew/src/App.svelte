<script>
  import { tick, onMount } from "svelte";
  import Tabs from "./UI/tabs.svelte";
  // import NavPanel from "./UI/nav.svelte";
  import Button from "./UI/Button.svelte";
  import Fa from "svelte-fa";
  import icons from "./icons.js";

  import HelpPage from "./pages/help.svelte";
  import Share from "./pages/share.svelte";
  import Connection from "./pages/connection.svelte";
  import Logs from "./pages/logs.svelte";
  import Settings from "./pages/settings.svelte";

  let mainid = "mainid";
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
  // let TABS;

  // $: console.log(TABS);

  onMount(() => {
    addtab({
      name: "share",
      description: "Shares",
      icon: icons.faFolderOpen
    });
    addtab({
      name: "connection",
      description: "Connections",
      icon: icons.faExchangeAlt
    });
    addtab({
      name: "settings",
      description: "Settings",
      icon: icons.faCog
    });
    addtab({ name: "logs", description: "Logs", icon: icons.faList });
    addtab({
      name: "help",
      description: "Help",
      icon: icons.faQuestionCircle
    });
    tick().then(() => (activetabid = tabs[0].id));
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
    }

    return tab;
  }

  function addtab(nav, args) {
    var e = document.getElementById(mainid);
    // create div
    var divtest = document.createElement("div");
    var id = "id" + Math.floor(Math.random() * 1000);
    divtest.id = id;
    divtest.className = "container active";
    e.appendChild(divtest);

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
      activetabid = id;
    });
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

    <Tabs {tabs} {activetabid} {mainid}/>

    <div id={mainid} />

  </div>
</div>
