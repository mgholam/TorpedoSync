<script>
  import { onMount } from "svelte";
  import msgbox from "../UI/msgbox.js";
  import Button from "../UI/button.svelte";

  export let active = false;
  let LogItems = [];
  let logtimersec = 5;
  let timer;

  onMount(() => {
    // create timer for refresh
    timer = setInterval(refresh, logtimersec * 1000);
    refresh(true);
  });

  function refresh(a) {
    if (a || active)
      window.GET("api/getlogs", function(d) {
        LogItems = d;
      });
  }
</script>

<style>
  .LogArea {
    min-height: 75vh;
    max-height: 75vh;
    border-style: solid;
    border-color: gray;
    background-color: #e0e0e0;
    padding: 5px;
    overflow: scroll;
  }

  .LogItems {
    font-family: "Courier New", monospace;
    white-space: pre-wrap;
    font-size: smaller;
  }
</style>

<svelte:options accessors={true} />

{#if active}
  <div class="tab-content" id="logs">
    <h2>Logs</h2>
    <p>Will be refreshed every {logtimersec} sec.</p>
    <Button on:click={refresh}>Refresh</Button>

    <div class="data">
      <div class="LogArea">
        {#each LogItems as i}
          <div class="LogItems">{i}</div>
        {/each}
      </div>
    </div>
  </div>
{/if}
