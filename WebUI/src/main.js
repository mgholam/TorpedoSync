import './js/extensions.js'
import './entities.js'
import './js/sweetalertmin.js'
import Vue from 'vue'
import VModal from 'vue-js-modal'
import App from './App.vue'

Vue.use(VModal)

require('./css/sweetalert.css')
require('./css/style.css')
require('./css/icons.css')
require('./css/glyphicons-halflings-regular.woff')

window.ServerURL = document.location.protocol + "//" + document.location.hostname + ":" + document.location.port + "/";
window.draggable = /windows/i.test(navigator.userAgent);

new Vue({
    el: '#app',
    render: h => h(App)
})