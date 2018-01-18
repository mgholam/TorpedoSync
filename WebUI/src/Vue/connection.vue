<template>
    <div>
        <button class="actionbutton" @click="connectto"><span class="gi gi-transfer" style="margin-right:10px;"></span>Connect To</button>
        <span v-if="connfilter.length>0">
            <label><span class="gi gi-filter"></span>Filter :</label>
            <input type="text" v-model="findconn" placeholder="Type here to filter the grid">
            <button @click="findconn=''">X</button>
            <label style="margin-left:5px;">Count = {{connfilter.length}}</label>
        </span>
        <div class="cssTbl">
            <table v-if="connfilter.length>0">
                <tr>
                    <th><a @click="sort('Name')">Share<span :class="sorticon('Name')"></span></a></th>
                    <th><a @click="sort('MachineName')">Machine<span :class="sorticon('MachineName')"></span></a></th>
                    <th><a @click="sort('Path')">Local Path<span :class="sorticon('Path')"></span></a></th>
                    <th><a @click="sort('isClient')">Type<span :class="sorticon('isClient')"></span></a></th>
                    <th><a @click="sort('isPaused')">Status<span :class="sorticon('isPaused')"></span></a></th>
                    <th><a>Actions</a></th>
                </tr>
                <tr v-for="(s,i) in connfilter">
                    <td><label><a @click="view(s)">{{s.Name}}</a></label></td>
                    <td>
                        <label>{{s.MachineName}}</label>
                    </td>
                    <td>
                        <label>{{s.Path}}</label>
                    </td>
                    <td>
                        <label>{{s.isClient?'Client':'Server'}} {{s.ReadOnly?'RO':'RW'}}</label>
                    </td>
                    <td>
                        <span :class="s.isPaused?'gi gi-pause':'gi gi-play'" :style="s.isPaused?'color:red;':'color:green;'"></span>
                        <label :style="s.isPaused?'color:red;':'color:green;'">{{ s.isPaused ? "Paused":"Running"}}</label>
                    </td>
                    <td>
                        <label><a class="al" @click="pause(s)"><span class="gi gi-pause"></span>Pause</a></label>
                        <label><a class="al" @click="resume(s)"><span class="gi gi-play"></span>Resume</a></label>
                        <label><a class="al" @click="remove(s)"><span class="gi gi-remove"></span>Disconnect</a></label>
                        <label v-if="s.isConfirmed===false">
                            <a v-if="s.isClient===false" class="al" @click="confirm(s)"><span class="gi gi-ok"></span>Confirm</a>
                            <a v-else>Waiting for Confirm</a>
                        </label>
                    </td>
                </tr>
            </table>
        </div>
        <connview :connection="selected" :draggable="draggable"></connview>
        <connectto @refresh="refresh" :draggable="draggable"></connectto>
    </div>
</template>

<script>
    import connview from './connview.vue'
    import connto from './connectto.vue'
    export default {
        data() {
            return {
                findconn: '',
                selected : {},
                sortcol: 'Name',
                sortAsc: true
            }
        },
        components: {
            "connview": connview,
            "connectto": connto
        },
        props: ['connections'],
        methods: {
            remove(conn) {
                var that = this;
                swal({
                    title: "Remove?",
                    text: "Do you want to remove this connection?",
                    showCancelButton: true,
                    closeOnConfirm: true,
                    cancelButtonText: 'Cancel',
                    animation: "slide-from-top"
                }, function(inputValue) {
                    if (inputValue === false) return false;
                    if (inputValue !== "") {
                        $.get2("api/connection.remove?" + conn.MachineName + "&" + conn.Name, function() {
                            that.$emit("refresh");
                        });
                    }
                });
            },
            sorticon(name) {
                if (this.sortcol === name) {
                    if (this.sortAsc === true)
                        return 's gi gi-sort-by-alphabet';
                    return 's gi gi-sort-by-alphabet-alt';
                }
                return '';
            },
            pause(conn) {
                var that = this;
                $.get2("api/connection.pauseresume?" + conn.MachineName + "&" + conn.Name + "&true", function() {
                    that.$emit("refresh");
                });
            },
            resume(conn) {
                var that = this;
                $.get2("api/connection.pauseresume?" + conn.MachineName + "&" + conn.Name + "&false", function() {
                    that.$emit("refresh");
                });
            },
            view(conn) {
                this.selected = conn;
                this.$modal.show("ConnModal");
            },
            confirm(conn) {
                var that = this;
                swal({
                    title: "Confirm?",
                    text: "Do you want to confirm this connection",
                    showCancelButton: true,
                    closeOnConfirm: true,
                    cancelButtonText: 'Cancel',
                    animation: "slide-from-top"
                }, function(inputValue) {
                    if (inputValue === false) return false;
                    if (inputValue !== "") {
                        //machinename & share & bool
                        $.get2("api/connection.confirm?" + conn.MachineName + "&" + conn.Name + "&true", function() {
                            that.$emit("refresh");
                        });
                    }
                });
            },
            sort(col) {
                if (col !== this.sortcol) {
                    this.sortAsc = true;
                    this.sortcol = col;
                } else
                    this.sortAsc = !this.sortAsc;
            },
            refresh() {
                this.$emit("refresh");
            },
            connectto() {
                this.$modal.show("ConnectToModal");
            }
        },
        computed: {
            connfilter() {
                return $.filter(this.connections, this.findconn, this.sortcol, this.sortAsc);
            },
            draggable(){
                return window.draggable;
            }
        }
    }
</script>

<style scoped>
    .al {
        text-decoration: underline;
        cursor: pointer;
        margin-right: 8px;
    }
    .s {
        margin-left: 8px;
    }
</style>