<template>
  <v-app style="background: radial-gradient(ellipse at bottom, #1b2735 0%, #090a0f 100%);" dark>
    <v-app-bar app color="black" dark>
      <div class="d-flex align-center">
        <v-img
          alt="SW Logo"
          class="shrink mr-2"
          contain
          :src="require('./assets/sw-logo.png')"
          transition="scale-transition"
          width="40"
        />
      </div>
    </v-app-bar>
    <v-content>
      <HelloWorld />
    </v-content>
  </v-app>
</template>

<script>
import HelloWorld from "./components/HelloWorld";

export default {
  name: "App",

  components: {
    HelloWorld
  },

  data: () => ({
    //
  })
};
</script>
