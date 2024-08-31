<script>
  export let active = false;     
</script>

<svelte:options accessors={true} />   
{#if active} 
<div class="tab-content" id="help">
  <h2>Definitions</h2>
  <p>
    To understand what is implemented in
    <code>TorpedoSync</code>
    you must be familiar with the following definitions:
  </p>
  <ul>
    <li>
      <strong>Share</strong>
      : is the folder you want to share
    </li>
    <li>
      <strong>State</strong>
      : is the list of files and folders
    </li>
    <li>
      <strong>Delta</strong>
      : is the result of comparing states for which you get a list of Added,
      Deleted, Changed files and Deleted Folders
    </li>
    <li>
      <strong>Queue</strong>
      : is the list of files to download for a delta
    </li>
    <li>
      <strong>Connection</strong>
      <ul>
        <li>
          <strong>Master</strong>
          Computer : is the computer which defined the share and the delta
          computation happens even in read/write mode
        </li>
        <li>
          <strong>Client</strong>
          Computer : is the computer connecting to a master which initiates a
          sync
        </li>
      </ul>
    </li>
  </ul>
  <h2>Rules</h2>
  <ul>
    <li>Master is the machine that defined the "share"</li>
    <li>
      Default is to move older or deleted files to the ".ts\old" folder in the
      share
    </li>
    <li>
      First time sync for Read/Write mode is to copy all missing files to both
      ends
    </li>
    <li>Only clients initiate sync even in read/write mode</li>
    <li>Delta processing is done on the master</li>
  </ul>
</div>
{/if}