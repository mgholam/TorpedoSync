<template>
    <div>
        <h3>Torpedo Sync version : {{configs.Version}}</h3>
        <h3 :style="stat()">Status : {{configs.PauseAll?'Paused': 'Running'}}</h3>
        <button v-if="configs.PauseAll===false" class="actionbutton" @click="pausets"><span class="gi gi-pause" style="margin-right:10px;"></span>Pause Torpedo Sync</button>
        <button v-else class="actionbutton" @click="resumets"><span class="gi gi-play" style="margin-right:10px;"></span>Resume Torpedo Sync</button>
        <button class="actionbutton" v-if="editmode===false" @click="edit"><span class="gi gi-edit" style="margin-right:10px;"></span>Edit Values</button>
        <button class="actionbutton" v-if="editmode===true" @click="cancel"><span class="gi gi-remove" style="margin-right:10px;"></span>Cancel</button>
        <button class="actionbutton" v-if="editmode===true" @click="save"><span class="gi gi-ok" style="margin-right:10px;"></span>Save Values</button>
        <div class="header" v-if="configs!==undefined&&configs.Globals!==undefined">
            <form>
                <table>
                    <tr>
                        <td>UDP port :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.Globals.UDPPort"></td>
                    </tr>
                    <tr>
                        <td>TCP port :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.Globals.TCPPort"></td>
                    </tr>
                    <tr>
                        <td>Web UI port :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.Globals.WebPort"></td>
                    </tr>
                    <tr>
                        <td>Local only Web UI :</td>
                        <td><input type="checkbox" v-model="configs.Globals.LocalOnlyWeb" class="chk" :disabled="!editmode"></td>
                    </tr>
                    <tr>
                        <td>Download Block Size in MB :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.Globals.DownloadBlockSizeMB"></td>
                    </tr>
                    <tr>
                        <td>Batch download files in a Zip :</td>
                        <td><input type="checkbox" v-model="configs.Globals.BatchZip" class="chk" :disabled="!editmode"></td>
                    </tr>
                    <tr>
                        <td>Batch Zip Files Under MB :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.Globals.BatchZipFilesUnderMB"></td>
                    </tr>
                    <tr>
                        <td>Auto start Web UI :</td>
                        <td><input type="checkbox" v-model="configs.Globals.StartWebUI" class="chk" :disabled="!editmode"></td>
                    </tr>
                    <tr>
                        <td>New Sync Timer Secs :</td>
                        <td><input type="number" :readonly="!editmode" v-model="configs.NewSyncTimerSecs"></td>
                    </tr>
                </table>
            </form>
        </div>
    </div>
</template>


<script>
        export default {
            props: ['configs'],
            data() {
                return {
                    editmode: false
                }
            },
            methods: {
                stat() {
                    if (this.configs.PauseAll === true)
                        return 'color:red;';
                    else
                        return 'color:green;';
                },
                cancel() {
                    this.editmode = false;
                    this.$emit("refresh");
                },
                save() {
                    // validate values
                    var msg = "";
                    var ok = true;
                    if(this.configs.Globals.UDPPort<1 || this.configs.Globals.UDPPort>65535) {ok=false; msg+="UDP port must be in range : 1-65535\r\n";}
                    if(this.configs.Globals.TCPPort<1 || this.configs.Globals.TCPPort>65535) {ok=false; msg+="TCP port must be in range : 1-65535\r\n";}
                    if(this.configs.Globals.WebPort<1 || this.configs.Globals.WebPort>65535) {ok=false; msg+="Web port must be in range : 1-65535\r\n";}
                    if(this.configs.Globals.DownloadBlockSizeMB<1 || this.configs.Globals.DownloadBlockSizeMB>200) {ok=false; msg+="Download block size must be in range : 1-200\r\n";}
                    if(this.configs.Globals.BatchZipFilesUnderMB<1 || this.configs.Globals.BatchZipFilesUnderMB>200) {ok=false; msg+="Batch size must be in range : 1-200\r\n";}
                    if(this.configs.NewSyncTimerSecs<5 || this.configs.NewSyncTimerSecs>3600) {ok=false; msg+="New sync timer must be in range : 5-3600\r\n";}
                    if(ok===false)
                    {
                        swal(msg);
                        return;
                    }
                    swal("not implemented yet");
                    this.editmode = false;
                    this.$emit("refresh");
                },
                edit() {
                    this.editmode = true;
                },
                pausets() {
                    var that = this;
                    $.get2("api/pausets?1", function() {
                        that.$emit("refresh");
                    });
                },
                resumets() {
                    var that = this;
                    $.get2("api/pausets?0", function() {
                        that.$emit("refresh");
                    });
                }
            }
        }
</script>

<style scoped>
    .header table tr td:first-child {
        width: 300px;
    }
    .chk {
        width: 30px;
        height: 30px;
    }
</style>