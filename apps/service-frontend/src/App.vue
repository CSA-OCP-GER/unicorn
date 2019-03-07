<template>
  <v-app>
    <v-toolbar app>
      <v-toolbar-title class="headline text-uppercase">
        <span>ISTIO</span>
        <span class="font-weight-light">JWT validation in the service mesh</span>
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn to="/about">Show me some data!</v-btn>
    </v-toolbar>
    <v-content>
      <router-view/>
    </v-content>
  </v-app>
</template>

<script>

export default {
  name: 'App',
  data () {
    return {
      //
    }
  }
}
</script>
