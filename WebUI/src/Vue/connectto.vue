<template>
    <modal name="ConnectToModal" :min-width="600" :min-height="180" :adaptive="true" :draggable="draggable" :resizable="true" height="180" transition="nice-modal-fade" classes="myclasses" @before-open="beforeOpen" @before-close="beforeClose">
        <div class="modalheader"><label>Connect To</label></div>
        <div class="modalcontainer">
            <div class="header">
                <form>
                    <table>
                        <tr>
                            <td>Connect To Token:</td>
                            <td><input type="text" v-model="connectTo.id" required placeholder="Get the token from the computer sharing"></td>
                        </tr>
                        <tr>
                            <td>Local Path :</td>
                            <td><input type="text" v-model="connectTo.path" required placeholder="e.g. C:\My Documents"></td>
                        </tr>
                    </table>
                </form>
            </div>
            <div class="buttonbar">
                <button v-if="isnew===true" class="actionbutton" @click="save">Connect</button>
                <button class="actionbutton" @click="close" style="float: right;">Close</button>
            </div>
            <div class="vue-modal-resizer"></div>
        </div>
        </div>
    </modal>
</template>

<script>
    export default {
        name: 'ConnectToModal',
        props:["draggable"],
        data() {
            return {
                allowclose: false,
                isnew: true,
                connectTo: {
                    id: '',
                    path: ''
                }
            }
        },
        methods: {
            save() {
                var ok = true;
                if (this.connectTo.id === '') ok = false;
                if (this.connectTo.path === '') ok = false;
                if (ok === false) {
                    swal('Please enter values');
                    return;
                }
                var that = this;
                $.get2("api/connect?" + this.connectTo.id + "&" + this.connectTo.path, function(ret) {
                    if (ret === false)
                        swal("Token not found on connected machines.");
                    that.allowclose = true;
                    that.$modal.hide('ConnectToModal');
                    that.$emit("refresh");
                });
            },
            close() {
                this.allowclose = true;
                this.$modal.hide('ConnectToModal');
            },
            beforeOpen(event) {
                this.allowclose = false;
                this.connectTo.id = '';
                this.connectTo.path = '';
            },
            beforeClose(event) {
                if (this.allowclose === false)
                    event.stop();
                else {
                    this.allowclose = false;
                    this.connectTo.id = '';
                    this.connectTo.path = '';
                }
            }
        },
        mounted() {
            this.allowclose = false;
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
    .modalcontainer {}
    .myclasses {
        border-radius: 20px;
        border: solid 2px #444444;
        padding: 10px;
        background: white;
        box-shadow: 0 2px 30px rgba(0, 0, 0, 1);
    }
    .v--modal-overlay[data-modal="ConnectToModal"] {
        background: rgba(0, 0, 0, 0.5);
    }
    .buttonbar {
        position: absolute;
        width: 95%;
        bottom: 0;
    }
    .vue-modal-resizer {
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