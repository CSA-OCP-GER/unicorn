var express = require("express");
var app = express();
var picard = require('picard-quotes');
var os = require('os');
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
}, 120 * 1000);

app.get("/api/quotes", (req, res, next) => {
    // randomly fail after 2 minutes from container start
    if (Math.floor(Math.random() * Math.floor(2)) > 0 && fail) {
        console.log("Failing on purpose.");
        return res.send(500, "Failed on purpose.");
    }
    picard.quote((quote) => {
        if (quote) {
            debugger;
            return res.json(
                {
                    quote: `Picard says: ${quote.quote}`,
                    host: os.hostname(),
                    image: process.env.IMAGE || 'local',
                    type: 'st'
                }
            );
        }
        return res.send(500, "Failed.");
    });
});

app.listen(3000, () => {
    console.log("Server running on port 3000");
});