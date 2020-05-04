<script>
  import { createEventDispatcher } from "svelte";
  import Modal from "../UI/Modal.svelte";
  import Button from "../UI/button.svelte";
  import msgbox from "../UI/msgbox.js";
  import u from "../utils.js";

  export let share; //= new Share();
  export let isnew = true;
  export let show = true;
  export let node;

  const dispatch = createEventDispatcher();

  function save() {
    var ok = true;
    share.Name = share.Name.trim();
    share.Path = share.Path.trim();
    if (share.Name == "") ok = false;
    if (share.Path == "") ok = false;
    if (ok == false) {
      msgbox.Ok("Please enter values", "Incomplete");
      return;
    }
    u.POST("api/share.add", share).then(close);
  }

  function close() {
    show = false;
    setTimeout(() => {
      document.body.removeChild(node);
    }, 1000);
    dispatch("close");
  }

  // share() {
  //     this.isnew = true;
  //     if (this.share.Name != "")
  //         this.isnew = false;
  // }
</script>

<style>

</style>

<svelte:options accessors={true} />

<Modal minwidth="600" minheight="300" title="Add a new Share" {show}>
  <div>
    <div class="header">
      <form>
        <table>
          <tr>
            <td>Unique Name :</td>
            <td>
              <input
                type="text"
                bind:value={share.Name}
                required
                placeholder="e.g. mydocs" />
            </td>
          </tr>
          <tr>
            <td>Local Path :</td>
            <td>
              <input
                type="text"
                bind:value={share.Path}
                required
                placeholder="e.g. C:\My Documents" />
            </td>
          </tr>
          <tr>
            <td>Read Only Token :</td>
            <td>
              <input type="text" readonly bind:value={share.ReadOnlyToken} />
            </td>
          </tr>
          <tr>
            <td>Read Write Token :</td>
            <td>
              <input type="text" readonly bind:value={share.ReadWriteToken} />
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
