import '@babel/polyfill'
import Vue from 'vue'
import './plugins/vuetify'
import App from './App.vue';
import router from './router';
import Adal from 'vue-adal';

Vue.config.productionTip = false

Vue.use(Adal, {
  config: {
    tenant: 'b2cdf8d6-6b34-4e87-a486-5f528fc1e4f9',
    clientId: '82f9b4ef-1b96-413e-86d0-b8d61e8d93f8',
    redirectUri: 'http://localhost:8080/callback',
    cacheLocation: 'localStorage'
  },
  requireAuthOnInitialize: true,
  router: router
})

new Vue({
  router,
  render: h => h(App)
}).$mount('#app')
