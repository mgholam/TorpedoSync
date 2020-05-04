<script>
  import { onMount } from "svelte";
  import msgbox from "../UI/msgbox.js";
  import Button from "../UI/button.svelte";
  import ViewShare from "./viewshare.svelte";
  import entity from "../entities.js";
  import Fa from "svelte-fa";
  import icons from "../icons.js";
  import u from "../utils.js";

  export let active = false;

  let shares = [];
  let selectedshare;
  let findshare = "";
  let sortcol = "Name";
  let sortAsc = true;
  let sharefilter;
  let draggable = false;

  $: sharefilter = u.FILTER(shares, findshare, sortcol, sortAsc);

  onMount(() => {
    refresh();
  });

  function sorticon(name, sc, sa) {
    if (sc == name) {
      if (sa == true) return icons.faSortAlphaDown;
      return icons.faSortAlphaDownAlt;
    }
    return "";
  }

  function refresh() {
    u.GET("api/getshares").then(data => {
      data.forEach((o, i) => (o.id = i));
      shares = data;
    });
  }

  function addshare() {
    var s = new entity.Share();
    // console.log(s);
    view(s, true);
  }

  function pause(share) {
    u.GET("api/share.pauseresume?" + share.Name + "&true").then(refresh);
  }

  function resume(share) {
    u.GET("api/share.pauseresume?" + share.Name + "&false").then(refresh);
  }

  function unshare(share, index) {
    msgbox.OkCancel("Do you want to remove this share?", "Remove?", () => {
      console.log("here");
      u.GET("api/share.remove?" + share.Name).then(refresh);
    });
  }

  function sort(col) {
    if (col != sortcol) {
      sortAsc = true;
      sortcol = col;
    } else sortAsc = !sortAsc;
  }

  function view(share, isnew) {
    var n = document.createElement("div");
    var e = document.body.appendChild(n);
    var ct = new ViewShare({
      target: n,
      props: { node: n, share, isnew }
    });
    ct.$on("close", e => refresh());
    ct.show = true;
  }
</script>

<style>
  .al {
    text-decoration: underline;
    cursor: pointer;
    margin-right: 8px;
  }
</style>

<svelte:options accessors={true} />

{#if active}
  <div class="tab-content">
    <h2>
      <Fa icon={icons.faFolderOpen} />
      Shares on this machine
    </h2>
    If you have a folder on this machine you want others to connect to, add that
    folder below.
    <br />
    <br />
    <Button on:click={addshare}>
      <Fa icon={icons.faPlus} />
      Add
    </Button>
    {#if shares.length > 0}
      <span>
        <label>
          <Fa icon={icons.faFilter} />
          Filter :
        </label>
        <input
          type="text"
          bind:value={findshare}
          placeholder="Type here to filter the grid" />
        <button on:click={() => (findshare = '')}>X</button>
        <label style="margin-left:5px;">Count = {sharefilter.length}</label>
      </span>
      <div class="cssTbl">
        <table>
          <tr>
            <th>
              <label on:click={() => sort('Name')}>
                Name
                <Fa icon={sorticon('Name', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label on:click={() => sort('Path')}>
                Path
                <Fa icon={sorticon('Path', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label>Actions</label>
            </th>
          </tr>
          {#each sharefilter as s, i (s.id)}
            <tr>
              <td>
                <label>
                  <label on:click={() => view(s, false)}>{s.Name}</label>
                </label>
              </td>
              <td>
                <label>{s.Path}</label>
              </td>
              <td>
                <label>
                  <label class="al" on:click={() => pause(s)}>
                    <Fa icon={icons.faPause} />
                    Pause
                  </label>
                </label>
                <label>
                  <label class="al" on:click={() => resume(s)}>
                    <Fa icon={icons.faPlay} />
                    Resume
                  </label>
                </label>
                <label>
                  <label class="al" on:click={() => unshare(s, i)}>
                    <Fa icon={icons.faTimes} />
                    Un-share
                  </label>
                </label>
              </td>
            </tr>
          {/each}
        </table>
      </div>
    {/if}
  </div>
{/if}
