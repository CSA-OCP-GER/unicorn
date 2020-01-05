<template>
  <v-app style="background: radial-gradient(ellipse at bottom, #1b2735 0%, #090a0f 100%);" dark>
    <v-content>
      <Quotes />
    </v-content>
  </v-app>
</template>

<script>
import Quotes from "./components/Quotes";

export default {
  name: "App",

  components: {
    Quotes
  },

  data: () => ({
    //
  })
};
</script>
