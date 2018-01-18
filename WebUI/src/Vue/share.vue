<template>
	<div>
		<button class="actionbutton" @click="addshare"><span class="gi gi-plus" style="margin-right:10px;"></span>Add</button>
		<span v-if="sharefilter.length>0">
			<label><span class="gi gi-filter"></span>Filter :</label>
			<input type="text" v-model="findshare" placeholder="Type here to filter the grid">
			<button @click="findshare=''">X</button>
			<label style="margin-left:5px;">Count = {{sharefilter.length}}</label>
		</span>
		<div class="cssTbl">
			<table v-if="sharefilter.length>0">
				<tr>
					<th><a @click="sort('Name')">Name<span :class="sorticon('Name')"></span></a></th>
					<th><a @click="sort('Path')">Path<span :class="sorticon('Path')"></span></a></th>
					<th><a>Actions</a></th>
				</tr>
				<tr v-for="(s,i) in sharefilter">
					<td><label><a @click="view(s)">{{s.Name}}</a></label></td>
					<td>
						<label>{{s.Path}}</label>
					</td>
					<td>
						<label><a class="al" @click="pause(s)"><span class="gi gi-pause"></span>Pause</a></label>
						<label><a class="al" @click="resume(s)"><span class="gi gi-play"></span>Resume</a></label>
						<label><a class="al" @click="unshare(s,i)"><span class="gi gi-remove"></span>Un-share</a></label>
					</td>
				</tr>
			</table>
		</div>
		<viewshare :share="selectedshare" @refresh="refresh" :draggable="draggable"></viewshare>
	</div>
</template>

<script>
	import addshare from './viewshare.vue'
	export default {
		data() {
			return {
				selectedshare: {},
				findshare: '',
				sortcol: 'Name',
				sortAsc: true
			}
		},
		components: {
			'viewshare': addshare
		},
		props: ['shares'],
		methods: {
			sorticon(name) {
				if (this.sortcol === name) {
					if (this.sortAsc === true)
						return 's gi gi-sort-by-alphabet';
					return 's gi gi-sort-by-alphabet-alt';
				}
				return '';
			},
			refresh() {
				this.$emit("refresh");
			},
			addshare() {
				this.selectedshare = new Share();
				this.$modal.show('AddShareModal');
			},
			pause(share) {
				var that = this;
				$.get2("api/share.pauseresume?" + share.Name + "&true", function() {
					that.$emit("refresh");
				});
			},
			resume(share) {
				var that = this;
				$.get2("api/share.pauseresume?" + share.Name + "&false", function() {
					that.$emit("refresh");
				});
			},
			unshare(share, index) {
				var that = this;
				swal({
					title: "Remove?",
					text: "Do you want to remove this share?",
					showCancelButton: true,
					closeOnConfirm: true,
					cancelButtonText: 'Cancel',
					animation: "slide-from-top"
				}, function(inputValue) {
					if (inputValue === false) return false;
					if (inputValue !== "") {
						$.get2("api/share.remove?" + share.Name, function() {
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
			view(share) {
				this.selectedshare = share;
				this.$modal.show("AddShareModal")
			}
		},
		computed: {
			sharefilter() {
				return $.filter(this.shares, this.findshare, this.sortcol, this.sortAsc);
			},
            draggable(){
                return window.draggable;
            }
		}
	}
</script>

<style>
	.al {
		text-decoration: underline;
		cursor: pointer;
		margin-right: 8px;
	}
	.s {
		margin-left: 8px;
	}
</style>