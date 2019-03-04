<template>
  <v-container>
    <v-layout row>
      <v-flex xs12 sm6 offset-sm3>
        <v-alert :value="true" type="success">You are successfully authenticated against Azure Active Directory.</v-alert>
        <v-card>
          <v-list two-line>
            <template v-for="(item, index) in tokenValues">
              <v-list-tile :key="item.key" @click>
                <v-list-tile-content>
                  <v-list-tile-title v-html="item.value"></v-list-tile-title>
                  <v-list-tile-sub-title v-html="item.key"></v-list-tile-sub-title>
                </v-list-tile-content>
              </v-list-tile>
            </template>
          </v-list>
        </v-card>
      </v-flex>
    </v-layout>
  </v-container>
</template>

<script>
import * as jwtDecode from "jwt-decode";

export default {
  data: () => ({
    token: {},
    tokenValues: []
  }),
  mounted() {
    var tokenkeys = localStorage.getItem("adal.token.keys");
    if (tokenkeys) {
      var tokenkey = tokenkeys.split("|")[0];
      var token = localStorage.getItem(`adal.access.token.key${tokenkey}`);
      if (token) {
        this.token = jwtDecode(token);
        this.tokenValues = [];
        for (var v in this.token) {
          this.tokenValues.push({ key: v, value: this.token[v] });
        }
      }
    }
  }
};
</script>
