<script>
  import { createEventDispatcher, onMount } from "svelte";
  import Modal from "../UI/Modal.svelte";
  import Button from "../UI/Button.svelte";
  import u from "../utils.js";

  export let connection;
  export let show = true;
  export let node;

  onMount(() => {
    refresh();
  });

  let info;
  let timer = null;
  let logtimerms = 1000;

  const dispatch = createEventDispatcher();

  function refresh() {
    if (timer == null) {
      timer = setInterval(refresh, logtimerms);
    }

    u.GET(
      "api/connection.getinfo?" + connection.MachineName + "&" + connection.Name
    ).then(data => (info = data));
  }

  function close() {
    if (timer) clearTimeout(timer);

    show = false;
    setTimeout(() => {
      document.body.removeChild(node);
    }, 1000);
    dispatch("close");
  }
</script>

<style>

</style>

<svelte:options accessors={true} />

<Modal title="Connection Information" minwidth="600" minheight="320" {show}>
  <div>
    {#if info != null}
      <div>
        <p>Total file count : {info.TotalFileCount.toLocaleString()}</p>
        <p>Files in queue : {info.FilesInQue.toLocaleString()}</p>
        <p>Failed files count : {info.FailedFiles.toLocaleString()}</p>
        <p>Queue Data Size : {info.QueDataSize.toLocaleString()} bytes</p>
        <p>mb/s : {info.Mbs}</p>
        <p>Estimated Time : {info.EstimatedTimeSecs} secs</p>
        <!-- <p>Last file copied : {info.LastFileNameDownloaded}</p> -->
      </div>
    {/if}
  </div>
  <footer slot="footer">
    <Button on:click={close}>Close</Button>
  </footer>
</Modal>
