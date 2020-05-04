<script>
  import { createEventDispatcher } from "svelte";
  import Modal from "../UI/Modal.svelte";
  import Button from "../UI/button.svelte";
  import msgbox from "../UI/msgbox.js";
  import u from "../utils.js";

  export let id = "";
  export let isnew = true;
  export let path = "";
  export let node;
  export let show = true;

  const dispatch = createEventDispatcher();

  function save() {
    var ok = true;
    id = id.trim();
    path = path.trim();
    if (id == "") ok = false;
    if (path == "") ok = false;
    if (ok == false) {
      msgbox.Ok("Please enter values", "Incomplete");
      return;
    }
    u.GET("api/connect?" + id + "&" + path).then(ret => {
      if (ret == false) {
        msgbox.Ok("Token not found on connected machines.", "Error");
        return;
      }
      close();
    });
  }

  function close() {
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

<Modal title="Connect To" minwidth="600" minheight="220" {show}>
  <div>
    <div class="header">
      <form>
        <table>
          <tr>
            <td>
              <label>Connect To Token:</label>
            </td>
            <td>
              <input
                type="text"
                bind:value={id}
                required
                placeholder="Get the token from the computer sharing" />
            </td>
          </tr>
          <tr>
            <td>
              <label>Local Path :</label>
            </td>
            <td>
              <input
                type="text"
                bind:value={path}
                required
                placeholder="e.g. C:\My Documents" />
            </td>
          </tr>
        </table>
      </form>
    </div>
  </div>
  <footer slot="footer">
    {#if isnew}
      <Button on:click={save}>Connect</Button>
    {/if}
    <Button on:click={close}>Close</Button>
  </footer>
</Modal>
