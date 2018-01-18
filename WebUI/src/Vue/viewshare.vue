<template>
    <modal name="AddShareModal" :min-width="600" :min-height="240" :adaptive="true" :draggable="draggable" :resizable="true" height="240" transition="nice-modal-fade" classes="myclasses" @before-open="beforeOpen" @before-close="beforeClose">
        <div class="modalheader"><label>Add a new Share</label></div>
        <div class="modalcontainer">
            <div class="header">
                <form>
                    <table>
                        <tr>
                            <td>Unique Name :</td>
                            <td><input type="text" v-model="share.Name" required placeholder="e.g. mydocs"></td>
                        </tr>
                        <tr>
                            <td>Local Path :</td>
                            <td><input type="text" v-model="share.Path" required placeholder="e.g. C:\My Documents"></td>
                        </tr>
                        <tr>
                            <td>Read Only Token :</td>
                            <td><input type="text" readonly v-model="share.ReadOnlyToken"></td>
                        </tr>
                        <tr>
                            <td>Read Write Token :</td>
                            <td><input type="text" readonly v-model="share.ReadWriteToken"></td>
                        </tr>
                    </table>
                </form>
            </div>
            <div class="buttonbar">
                <button v-if="isnew===true" class="actionbutton" @click="save">Save</button>
                <button class="actionbutton" @click="close" style="float: right;">Close</button>
            </div>
            <div class="vue-modal-resizer"></div>
        </div>
        </div>
    </modal>
</template>

<script>
    export default {
        name: 'AddShareModal',
        props: ["share", "draggable"],
        data() {
            return {
                allowclose: false,
                isnew: true
            }
        },
        methods: {
            save() {
                var ok = true;
                if (this.share.Name === '') ok = false;
                if (this.share.Path === '') ok = false;
                if (ok === false) {
                    swal('Please enter values');
                    return;
                }
                var that = this;
                $.post2("api/share.add", this.share, function(data) {
                    that.allowclose = true;
                    that.$modal.hide('AddShareModal');
                    that.$emit("refresh");
                });
            },
            close() {
                this.allowclose = true;
                this.$modal.hide('AddShareModal');
            },
            beforeOpen(event) {
                this.allowclose = false;
            },
            beforeClose(event) {
                if (this.allowclose === false)
                    event.stop();
                else {
                    this.allowclose = false;
                }
            }
        },
        watch: {
            share() {
                this.isnew = true;
                if (this.share.Name !== "")
                    this.isnew = false;
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
    .v--modal-overlay[data-modal="AddShareModal"] {
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