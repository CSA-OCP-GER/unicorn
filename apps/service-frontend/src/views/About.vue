<template>
  <v-layout align-start row wrap>
    <v-flex xs12>
      <v-card>
        <v-data-table :headers="headers" :items="results" :rows-per-page-items="[25]">
          <template slot="items" slot-scope="props">
            <td>{{ props.item.company }}</td>
            <td>{{ props.item.address }}</td>
            <td>{{ props.item.name.first }}</td>
            <td>{{ props.item.name.last }}</td>
            <td>{{ props.item.email }}</td>
          </template>
        </v-data-table>
      </v-card>
    </v-flex>
  </v-layout>
</template>
<script>
import axios from "axios";

export default {
  mounted() {
    var tokenkeys = localStorage.getItem("adal.token.keys");
    if (tokenkeys) {
      var tokenkey = tokenkeys.split("|")[0];
      this.token = localStorage.getItem(`adal.access.token.key${tokenkey}`);
      if (this.token) {
        axios
          .get("http://localhost:3099/data", {
            headers: { Authorization: `Bearer ${this.token}` }
          })
          .then(response => {
            this.results = response.data;
          });
      }
    }
  },
  data: () => {
    return {
      headers: [
        {
          text: "Company",
          align: "left",
          sortable: true,
          value: "company"
        },
        { text: "Address", value: "address" },
        { text: "Firstname", value: "firstname" },
        { text: "Lastname", value: "lastname" },
        { text: "Email", value: "email" }
      ],
      results: [],
      token: ""
    };
  }
};
</script>

