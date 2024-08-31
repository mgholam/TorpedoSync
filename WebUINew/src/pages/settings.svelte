<script>
  import { onMount } from "svelte";
  import msgbox from "../UI/msgbox.js";
  import Button from "../UI/Button.svelte";
  import Fa from "svelte-fa";
  import icons from "../icons.js";
  import u from "../utils.js";

  export let active = false;

  let configs = {};
  let editmode = false;
  let themecolor = "#999";

  $: changetheme(themecolor);

  function changetheme(color) {
    // msgbox.setColor(color);
    //msgbox.Ok("Theme changed!", "");
    if (color != "#999") {
      // console.log(color);
      document.documentElement.style.setProperty(
        "--theme-darker",
        // LightenDarkenColor(color, -66)
        hexToHSL(color, -0.3)
      );
      document.documentElement.style.setProperty(
        "--theme-dark",
        // LightenDarkenColor(color, -33)
        hexToHSL(color, -0.1)
      );
      document.documentElement.style.setProperty("--theme-color", color);
      document.documentElement.style.setProperty(
        "--theme-light",
        // LightenDarkenColor(color, 33)
        hexToHSL(color, 0.1)
      );
      document.documentElement.style.setProperty(
        "--theme-lighter",
        // LightenDarkenColor(color, 66)
        hexToHSL(color, 0.3)
      );
    } else {
      document.documentElement.style.setProperty("--theme-darker", "#666");
      document.documentElement.style.setProperty("--theme-dark", "#777");
      document.documentElement.style.setProperty("--theme-color", "#999");
      document.documentElement.style.setProperty("--theme-light", "#ccc");
      document.documentElement.style.setProperty("--theme-lighter", "#eee");
    }
  }

  function hexToHSL(H, alpha) {
    // Convert hex to RGB first
    let r = 0,
      g = 0,
      b = 0;
    if (H.length == 4) {
      r = "0x" + H[1] + H[1];
      g = "0x" + H[2] + H[2];
      b = "0x" + H[3] + H[3];
    } else if (H.length == 7) {
      r = "0x" + H[1] + H[2];
      g = "0x" + H[3] + H[4];
      b = "0x" + H[5] + H[6];
    }
    // Then to HSL
    r /= 255;
    g /= 255;
    b /= 255;
    let cmin = Math.min(r, g, b),
      cmax = Math.max(r, g, b),
      delta = cmax - cmin,
      h = 0,
      s = 0,
      l = 0;

    if (delta == 0) h = 0;
    else if (cmax == r) h = ((g - b) / delta) % 6;
    else if (cmax == g) h = (b - r) / delta + 2;
    else h = (r - g) / delta + 4;

    h = Math.round(h * 60);

    if (h < 0) h += 360;

    l = (cmax + cmin) / 2;
    s = delta == 0 ? 0 : delta / (1 - Math.abs(2 * l - 1));
    s = +(s * 100).toFixed(1);
    l += alpha;
    l = +(l * 100).toFixed(1);

    return "hsl(" + h + "," + s + "%," + l + "%)";
  }

  onMount(() => {
    refresh();
  });

  function cancel() {
    editmode = false;
    refresh();
  }

  function save() {
    // validate values
    var msg = "";
    var ok = true;
    if (configs.Globals.UDPPort < 1 || configs.Globals.UDPPort > 65535) {
      ok = false;
      msg += "UDP port must be in range : 1-65535\r\n";
    }
    if (configs.Globals.TCPPort < 1 || configs.Globals.TCPPort > 65535) {
      ok = false;
      msg += "TCP port must be in range : 1-65535\r\n";
    }
    if (configs.Globals.WebPort < 1 || configs.Globals.WebPort > 65535) {
      ok = false;
      msg += "Web port must be in range : 1-65535\r\n";
    }
    if (
      configs.Globals.DownloadBlockSizeMB < 1 ||
      configs.Globals.DownloadBlockSizeMB > 200
    ) {
      ok = false;
      msg += "Download block size must be in range : 1-200\r\n";
    }
    if (
      configs.Globals.BatchZipFilesUnderMB < 1 ||
      configs.Globals.BatchZipFilesUnderMB > 200
    ) {
      ok = false;
      msg += "Batch size must be in range : 1-200\r\n";
    }
    if (configs.NewSyncTimerSecs < 5 || configs.NewSyncTimerSecs > 3600) {
      ok = false;
      msg += "New sync timer must be in range : 5-3600\r\n";
    }
    if (ok == false) {
      msgbox.Ok(msg);
      return;
    }
    msgbox.Ok("not implemented yet");
    editmode = false;
    refresh();
  }

  function edit() {
    editmode = true;
  }

  function pausets() {
    u.GET("api/pausets?1").then(refresh);
  }
  function resumets() {
    u.GET("api/pausets?0").then(refresh);
  }

  function refresh() {
    u.GET("api/getconfigs").then(data => (configs = data));
  }
</script>

<style>
  .header table tr td:first-child {
    min-width: 240px;
  }
</style>

<svelte:options accessors={true} />

{#if active}
  <div class="tab-content">
    <h3>Torpedo Sync version : {configs.Version}</h3>
    <h3 style={configs.PauseAll ? 'color:red;' : 'color:green;'}>
      Status : {configs.PauseAll ? 'Paused' : 'Running'}
    </h3>
    {#if configs.PauseAll == false}
      <Button on:click={pausets}>
        <Fa icon={icons.faPause} />
        Pause Torpedo Sync
      </Button>
    {:else}
      <Button on:click={resumets}>
        <Fa icon={icons.faPlay} />
        Resume Torpedo Sync
      </Button>
    {/if}
    {#if editmode == false}
      <Button on:click={edit}>
        <Fa icon={icons.faEdit} />
        Edit Values
      </Button>
    {:else}
      <Button on:click={cancel}>
        <Fa icon={icons.faTimes} />
        Cancel
      </Button>
      <Button on:click={save}>
        <Fa icon={icons.faSave} />
        Save Values
      </Button>
    {/if}
    <!-- <br />
    <label>Theme color :</label>
    <input type="color" name="favcolor" bind:value={themecolor} />
    <br /> -->
    {#if configs && configs.Globals}
      <div class="header">
        <form>
          <table>
            <tr>
              <td>UDP port :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.Globals.UDPPort} />
              </td>
            </tr>
            <tr>
              <td>TCP port :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.Globals.TCPPort} />
              </td>
            </tr>
            <tr>
              <td>Web UI port :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.Globals.WebPort} />
              </td>
            </tr>
            <tr>
              <td>Local only Web UI :</td>
              <td>
                <input
                  type="checkbox"
                  checked={configs.Globals.LocalOnlyWeb}
                  bind:value={configs.Globals.LocalOnlyWeb}
                  disabled={!editmode} />
              </td>
            </tr>
            <tr>
              <td>Download Block Size in MB :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.Globals.DownloadBlockSizeMB} />
              </td>
            </tr>
            <tr>
              <td>Batch download files in a Zip :</td>
              <td>
                <input
                  type="checkbox"
                  checked={configs.Globals.BatchZip}
                  bind:value={configs.Globals.BatchZip}
                  disabled={!editmode} />
              </td>
            </tr>
            <tr>
              <td>Batch Zip Files Under MB :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.Globals.BatchZipFilesUnderMB} />
              </td>
            </tr>
            <tr>
              <td>Auto start Web UI :</td>
              <td>
                <input
                  type="checkbox"
                  checked={configs.Globals.StartWebUI}
                  bind:value={configs.Globals.StartWebUI}
                  disabled={!editmode} />
              </td>
            </tr>
            <tr>
              <td>New Sync Timer Secs :</td>
              <td>
                <input
                  type="number"
                  readonly={!editmode}
                  bind:value={configs.NewSyncTimerSecs} />
              </td>
            </tr>
          </table>
        </form>
      </div>
    {/if}
  </div>
{/if}
