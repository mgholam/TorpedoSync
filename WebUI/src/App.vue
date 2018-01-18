<template>
  <div id="app">
    <label style="float:right;color:gray;font-size:small">{{server}}</label>
    <div class="Container">
      <label class="brand">Torpedo Sync</label>
      <div class="middle">
        <ul class="tab-links">
          <li id="shares">
            <div @click="show('shares',this)">
              <a><span class="gi gi-folder-open" style="margin-right:10px;"></span>Shares</a>
            </div>
          </li>
          <li id="connections">
            <div @click="show('connections', this)">
              <a><span class="gi gi-transfer" style="margin-right:10px;"></span>Connections</a>
            </div>
          </li>
          <li id="settings">
            <div @click="show('settings',this)">
              <a><span class="gi gi-cog" style="margin-right:10px;"></span>Settings</a>
            </div>
          </li>
          <li id="logs">
            <div @click="show('logs',this)">
              <a><span class="gi gi-list-alt" style="margin-right:10px;"></span>Logs</a>
            </div>
          </li>
          <li id="help">
            <div @click="show('help',this)">
              <a><span class="gi gi-question-sign" style="margin-right:10px;"></span>Help</a>
            </div>
          </li>
        </ul>


        <div class="tab-content" id="shares">
          <h2><span class="gi gi-folder-open" style="margin-right:10px;"></span>Shares on this machine</h2>
          If you have a folder on this machine you want others to connect to, add that folder below.
          <br/>
          <br/>
          <share :shares="shares" @refresh="refresh" />
        </div>
        
        <div class="tab-content" id="connections">
          <h2><span class="gi gi-transfer" style="margin-right:10px;"></span>Connections to other machines</h2>
          <connection :connections="connections" @refresh="refresh"></connection>
        </div>
        
        <div class="tab-content" id="settings">
          <settings :configs="configs" @refresh="refresh"></settings>
        </div>
        
        <div class="tab-content" id="logs">
          <h2>Logs</h2>
          <p>Will be refreshed every {{logtimersec}} sec.</p>
          <button @click="refresh" class="actionbutton">Refresh</button>
          <div class="data" v-if="LogItems.length>0">
            <div class="LogArea">
              <div class="LogItems" v-for="i in LogItems">{{i}}</div>
            </div>
          </div>
        </div>
        
        <div class="tab-content" id="help">
          <h2>Definitions</h2>
          <p>To understand what is implemented in<code>TorpedoSync</code> you must be familiar with the following definitions:</p>
          <ul>
            <li>
              <strong>Share</strong> : is the folder you want to share
            </li>
            <li>
              <strong>State</strong> : is the list of files and folders
            </li>
            <li>
              <strong>Delta</strong> : is the result of comparing states for which you get a list of Added, Deleted, Changed files and Deleted Folders
            </li>
            <li>
              <strong>Queue</strong> : is the list of files to download for a delta
            </li>
            <li>
              <strong>Connection</strong>
              <ul>
                <li><strong>Master</strong> Computer : is the computer which defined the share and the delta computation happens even in read/write mode</li>
                <li><strong>Client</strong> Computer : is the computer connecting to a master which initiates a sync</li>
              </ul>
            </li>
          </ul>
          <h2>Rules</h2>
          <ul>
            <li>Master is the machine that defined the "share"</li>
            <li>Default is to move older or deleted files to the ".ts\old" folder in the share</li>
            <li>First time sync for Read/Write mode is to copy all missing files to both ends</li>
            <li>Only clients initiate sync even in read/write mode</li>
            <li>Delta processing is done on the master</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import share from './Vue/share.vue'
  import connecton from './Vue/connection.vue'
  import settings from './Vue/settings.vue'
  export default {
    name: 'app',
    components: {
      'share': share,
      'connection': connecton,
      'settings': settings
    },
    data() {
      return {
        shares: [],
        connections: [],
        server: '',
        LogItems: [],
        timer: '',
        logtimersec: 5,
        configs: {},
        filled: false //,
      }
    },
    methods: {
      show(tab) {
        $(".tab-content").hide();
        $(".tab-links li").each(function(i) {
          this.className = "";
        });
        $('.tab-content#' + tab).first().style.display = "block";
        $('.tab-links li#' + tab).first().className = "active";
      },
      refresh() {
        this.server = window.ServerURL;
        
        if (this.timer === '') {
          this.timer = setInterval(this.refresh, this.logtimersec * 1000);
        }
        if ($.showingdialog === true)
          return;
        var that = this;
        $.get2("api/getshares", function(data) {
          that.shares = data;
        });
        $.get2("api/getconnections", function(data) {
          that.connections = data;
        });
        $.get2("api/getconfigs", function(data) {
          that.configs = data;
        });
        $.get2("api/getlogs", function(d) {
          that.LogItems = d;
        });
      }
    },
    updated() {
      if (this.filled == false) {
        this.filled = true;
        this.refresh();
      }
    },
    mounted() {
      this.show('shares');
      this.refresh();
    }
  }
</script>

<style>
  .brand {
    color: white;
    font-size: x-large;
    float: left;
    padding: 4px;
    margin-right: 5px;
  }
  .LogArea {
    min-height: 75vh;
    max-height: 75vh;
    overflow: scroll;
  }
</style>