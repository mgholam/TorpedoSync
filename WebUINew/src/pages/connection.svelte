<script>
  import { onMount } from "svelte";
  import msgbox from "../UI/msgbox.js";
  import Button from "../UI/button.svelte";
  import ConnectTo from "./connectto.svelte";
  import ConnectInfo from "./connview.svelte";
  import Fa from "svelte-fa";
  import icons from "../icons.js";
  import u from "../utils.js";

  export let active = false;

  let connections = [];
  let findconn = "";
  let selected;
  let sortcol = "Name";
  let sortAsc = true;

  let connfilter = [];

  $: connfilter = u.FILTER(connections, findconn, sortcol, sortAsc);
  $: if (active) refresh();

  onMount(() => {
    refresh();
  });

  function remove(conn) {
    msgbox.OkCancel("Do you want to remove this connection?", "Remove?", () => {
      u.GET("api/connection.remove?" + conn.MachineName + "&" + conn.Name).then(
        refresh
      );
    });
  }

  function sorticon(name, sc, sa) {
    if (sc == name) {
      if (sa == true) return icons.faSortAlphaDown;
      return icons.faSortAlphaDownAlt;
    }
    return "";
  }

  function pause(conn) {
    // console.log(conn);
    u.GET(
      "api/connection.pauseresume?" +
        conn.MachineName +
        "&" +
        conn.Name +
        "&true"
    ).then(refresh);
  }

  function resume(conn) {
    u.GET(
      "api/connection.pauseresume?" +
        conn.MachineName +
        "&" +
        conn.Name +
        "&false"
    ).then(refresh);
  }

  function view(conn, isnew) {
    var n = document.createElement("div");
    var e = document.body.appendChild(n);
    var ct = new ConnectInfo({
      target: n,
      props: { node: n, connection: conn }
    });
    ct.show = true;
    ct.$on("close", e => refresh());
  }

  function confirm(conn) {
    msgbox.OkCancel(
      "Do you want to confirm this connection",
      "Confirm?",
      () => {
        //machinename & share & bool
        u.GET(
          "api/connection.confirm?" +
            conn.MachineName +
            "&" +
            conn.Name +
            "&true"
        ).then(refresh);
      }
    );
  }

  function sort(col) {
    if (col != sortcol) {
      sortAsc = true;
      sortcol = col;
    } else sortAsc = !sortAsc;
  }

  function refresh() {
    u.GET("api/getconnections").then(data => {
      data.forEach((o, i) => (o.id = i));
      connections = data;
      // console.log( connections);
    });
  }

  function connectto() {
    var n = document.createElement("div");
    var e = document.body.appendChild(n);
    var ct = new ConnectTo({
      target: n,
      props: { node: n, id: "", path: "" }
    });
    ct.show = true;
    ct.$on("close", e => refresh());
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
      <Fa icon={icons.faExchangeAlt} />
      Connections to other machines
    </h2>
    <Button on:click={connectto}>
      <Fa icon={icons.faExchangeAlt} />
      Connect To
    </Button>
    {#if connections.length > 0}
      <span>
        <label>
          <Fa icon={icons.faFilter} />
          Filter :
        </label>
        <input
          type="text"
          bind:value={findconn}
          placeholder="Type here to filter the grid" />
        <button on:click={() => (findconn = '')}>X</button>
        <label style="margin-left:5px;">Count = {connfilter.length}</label>
      </span>
      <div class="cssTbl">
        <table>
          <tr>
            <th on:click={() => sort('Name')}>
              <label>
                Share
                <Fa icon={sorticon('Name', sortcol, sortAsc)} />
              </label>
            </th>
            <th on:click={() => sort('MachineName')}>
              <label>
                Machine
                <Fa icon={sorticon('MachineName', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label on:click={() => sort('Path')}>
                Local Path
                <Fa icon={sorticon('Path', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label on:click={() => sort('isClient')}>
                Type
                <Fa icon={sorticon('isClient', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label on:click={() => sort('isPaused')}>
                Status
                <Fa icon={sorticon('isPaused', sortcol, sortAsc)} />
              </label>
            </th>
            <th>
              <label>Actions</label>
            </th>
          </tr>
          {#each connfilter as s (s.id)}
            <tr>
              <td>
                <label>
                  <label on:click={() => view(s, false)}>{s.Name}</label>
                </label>
              </td>
              <td>
                <label>{s.MachineName}</label>
              </td>
              <td>
                <label>{s.Path}</label>
              </td>
              <td>
                <label>
                  {s.isClient ? 'Client' : 'Server'} {s.ReadOnly ? 'RO' : 'RW'}
                </label>
              </td>
              <td>
                <Fa
                  icon={s.isPaused ? icons.faPause : icons.faPlay}
                  color={s.isPaused ? 'red' : 'green'} />
                <label style={s.isPaused ? 'color:red;' : 'color:green;'}>
                  {s.isPaused ? 'Paused' : 'Running'}
                </label>
              </td>
              <td>
                {#if s.isPaused == false}
                  <label class="al" on:click={() => pause(s)}>
                    <Fa icon={icons.faPause} />
                    Pause
                  </label>
                {:else}
                  <label class="al" on:click={() => resume(s)}>
                    <Fa icon={icons.faPlay} />
                    Resume
                  </label>
                {/if}
                <label class="al" on:click={() => remove(s)}>
                  <Fa icon={icons.faTimes} />
                  Disconnect
                </label>
                {#if s.isConfirmed == false}
                  {#if s.isClient == false}
                    <label class="al" on:click={() => confirm(s)}>
                      <span class="gi gi-ok" />
                      Confirm
                    </label>
                  {:else}
                    <label v-else>Waiting for Confirm</label>
                  {/if}
                {/if}
              </td>
            </tr>
          {/each}
        </table>
      </div>
    {/if}
  </div>
{/if}
