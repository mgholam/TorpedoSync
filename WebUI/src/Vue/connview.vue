<template>
    <modal name="ConnModal" 
        :min-width="600"
        :min-height="320"
        :adaptive="true"
        :draggable="draggable"
        :resizable="true"
        height="320"
        transition="nice-modal-fade" 
        classes="myclasses" 
        @before-open="beforeOpen" 
        @before-close="beforeClose">

        <div class="modalheader"><label>Connection Information</label></div>
            <div class="modalcontainer">
                <div v-if="info!==undefined">
                    <p>Total file count : {{info.TotalFileCount.toLocaleString()}}</p>
                    <p>Files in queue : {{info.FilesInQue.toLocaleString()}}</p>
                    <p>Failed files count : {{info.FailedFiles.toLocaleString()}}</p>
                    <p>Queue Data Size : {{info.QueDataSize.toLocaleString()}} bytes</p>
                    <p>mb/s : {{info.Mbs}}</p>
                    <p>Estimated Time : {{info.EstimatedTimeSecs}} secs</p>
                    <p>Last file copied : {{info.LastFileNameDownloaded}}</p>
                </div>

                <div class="buttonbar">
                    <button class="actionbutton" @click="close" style="float: right;">Close</button>
                </div>
                <div class="vue-modal-resizer"></div>
            </div>
        </div>
    </modal>
</template>

<script>
    export default {
        name: 'ConnModal',
        props:["connection", "draggable"],
        data() {
            return {
                allowclose: false, 
                timer: '',
                logtimerms: 500,
                info : undefined
            }
        },

        methods: {
            drag(){
                return /windows/i.test(navigator.userAgent);
            },
            refresh(){
                if (this.timer === '') {
                    this.timer = setInterval(this.refresh, this.logtimerms);
                }
                if ($.showingdialog === true)
                    return;
                var that = this;
                $.get2("api/connection.getinfo?" + this.connection.MachineName + "&" + this.connection.Name, function(data){
                    that.info = data;
                });
            },
            close() {
                this.allowclose = true;
                this.$modal.hide('ConnModal');
            },
            beforeOpen(event) {
                this.allowclose = false;
            },

            beforeClose(event) {
                if(this.allowclose===false)
                    event.stop();
                else{
                    clearInterval(this.timer);
                    this.timer = '';
                    this.allowclose = false;
                }
            }
        },
        watch:{
            connection(){
                this.refresh();
            }
        },
        mounted() {
            this.allowclose=false;
        }
    }
</script>

<style>
    .modalheader {
        background: darkgrey;
        padding: 5px;
        text-align: center;
        margin: -10px;
        margin-bottom: 5px;
    }
    
    .modalcontainer {
    }
    
    .myclasses {
        border-radius: 20px;
        border: solid 2px #444444;
        padding: 10px;
        background : white;
        box-shadow: 0 2px 30px rgba(0, 0, 0, 1);
    }

    .v--modal-overlay[data-modal="ConnModal"] {
        background: rgba(0, 0, 0, 0.5);
    }
    .buttonbar {
        position: absolute;
        width: 95%; 
        bottom:0;
    }
    .vue-modal-resizer{
        display: block;
        overflow: hidden;
        position: absolute;
        width: 12px;
        height: 12px;
        right: 0;
        bottom: 0;
        z-index: 9999999;
        background: red;
        cursor: se-resize;
    }
</style>