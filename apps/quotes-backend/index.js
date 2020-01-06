var express = require("express");
var os = require('os');
var app = express();
var starwars = require('starwars');
var fail = false;
var cors = require('cors');
app.use(cors());

setTimeout(() => {
    var enabled = process.env.FAIL_ENABLED == "1" || false;
    fail = enabled;
    if (fail) {
        console.log("NOW: failing on purpose.");
    } else {
        console.log("Failing on purpose disabled.");
    }
}, 6 * 1000);

app.get("/api/quotes", (req, res, next) => {
    // randomly fail after 2 minutes from container start
    if (Math.floor(Math.random() * Math.floor(2)) > 0 && fail) {
        console.log("Failing on purpose.");
        return res.send(500, "Failed on purpose.");
    }

    return res.json(
        {
            quote: starwars(),
            host: os.hostname(),
            image: process.env.IMAGE || 'local',
            type: 'sw'
        }
    );
});

app.listen(3000, () => {
    console.log("Server running on port 3000");
});