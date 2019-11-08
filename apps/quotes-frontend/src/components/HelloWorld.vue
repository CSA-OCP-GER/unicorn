<template>
  <v-container style="z-index: 100;">
    <v-layout text-center wrap>
      <v-flex style="z-index: 100;" xs12>
        <v-img :src="require('../assets/sw-logo.png')" class="my-3" contain height="200"></v-img>
      </v-flex>

      <v-flex xs12 style="z-index: 100;" mb-4>
        <h2 class="display-1 font-weight-bold mb-3">Welcome to the famous Star Wars Quotes app!</h2>
        <v-btn @click="reload()" :disabled="looping" class="primary" x-large>Load New Quote</v-btn>
      </v-flex>
      <v-flex class="pt-7" style="z-index: 100;" xs12>
        <p>Frequency: {{ms}} ms</p>
        <v-slider
          :disabled="looping"
          class="pt-4"
          thumb-label
          v-model="ms"
          :min="100"
          :max="5000"
          step="100"
          ticks
        ></v-slider>
      </v-flex>
      <v-flex style="z-index: 100;" xs12>
        <v-btn
          v-if="!looping"
          @click="loop()"
          class="success"
          :loading="looping"
          x-large
        >Load in Loop</v-btn>
        <v-btn v-if="looping" @click="stoploop()" class="warning" x-large>Stop</v-btn>
      </v-flex>
      <v-progress-linear
        v-if="looping"
        class="mt-4"
        color="white"
        style="z-index: 100;"
        indeterminate
      ></v-progress-linear>
      <v-flex xs12 class="pt-10" style="z-index: 100;" mb-4>
        <v-alert
          v-if="errorCode == false"
          icon="mdi-voice"
          type="success"
          dark
          border="left"
          prominent
        >
          <h1>{{quote}}</h1>
        </v-alert>
        <v-alert v-else icon="mdi-cancel" type="error" dark border="left" prominent>
          <h1>Error occured!</h1>
        </v-alert>
      </v-flex>
      <v-flex xs12>
        <div class="stars"></div>
        <div class="twinkling"></div>
        <div class="clouds"></div>
      </v-flex>
    </v-layout>
  </v-container>
</template>

<script>
import axios from "axios";

export default {
  mounted() {
    this.reload();
  },
  data() {
    return {
      looping: false,
      quote: "",
      errorCode: false,
      interval: null,
      loading: false,
      ms: 3000
    };
  },
  methods: {
    reload() {
      if (this.loading) return;
      this.loading = true;

      axios
        .get(window.uisettings.endpoint)
        .then(res => {
          this.errorCode = false;
          this.quote = res.data.quote;
          this.loading = false;
        })
        .catch(() => {
          this.errorCode = true;
          this.loading = false;
        });
    },
    loop() {
      this.looping = true;
      this.interval = setInterval(() => {
        this.reload();
      }, this.ms);
    },
    stoploop() {
      this.looping = false;
      clearInterval(this.interval);
    }
  }
};
</script>
