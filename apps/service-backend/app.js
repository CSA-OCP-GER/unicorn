'use strict';
if (process.env.ENVIRONMENT && process.env.ENVIRONMENT.toLowerCase() != 'production') var env = require('dotenv').config();
var jwt = require('express-jwt');
var SwaggerExpress = require('swagger-express-mw');
var app = require('express')();
var jwksRsa = require('jwks-rsa');

var config = {
  appRoot: __dirname
};

const checkJwt = jwt({
  secret: jwksRsa.expressJwtSecret({
    cache: true,
    rateLimit: true,
    jwksRequestsPerMinute: 5,
    jwksUri: process.env.JWKS_URI
  }),
  audience: process.env.AUDIENCE,
  issuer: process.env.ISSUER,
  algorithms: ['RS256']
});

app.use("*", checkJwt);

SwaggerExpress.create(config, function (err, swaggerExpress) {
  if (err) { throw err; }

  // install middleware
  swaggerExpress.register(app);

  app.use(function (err, req, res, next) {
    if (err.name === 'UnauthorizedError') {
      return res.status(401).json({ code: '401', message: 'Ivalid token. Sent from NODE app.' }).end();
    }
    if (err.code === 'invalid_token') {
      return res.status(401).json({ code: '401', message: err.message }).end();
    }
  });

  var port = process.env.PORT || 3000;
  app.listen(port);
});
