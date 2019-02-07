'use strict';
var jwt = require('express-jwt');
var SwaggerExpress = require('swagger-express-mw');
var app = require('express')();
var jwksRsa = require('jwks-rsa');
module.exports = app; // for testing

var config = {
  appRoot: __dirname // required config
};

const checkJwt = jwt({
  secret: jwksRsa.expressJwtSecret({
    cache: true,
    rateLimit: true,
    jwksRequestsPerMinute: 5,
    jwksUri: 'https://login.microsoftonline.com/common/discovery/v2.0/keys'
  }),
  audience: '82f9b4ef-1b96-413e-86d0-b8d61e8d93f8',
  issuer: 'https://sts.windows.net/b2cdf8d6-6b34-4e87-a486-5f528fc1e4f9/',
  algorithms: ['RS256']
});

app.use("*", checkJwt);

app.use(function (err, req, res, next) {
  if (err.name === 'UnauthorizedError') {
    res.status(401).json({ code: '401', message: 'Ivalid token. Sent from NODE app.' });
  }
  if (err.code === 'invalid_token') {
    res.status(401).json({ code: '401', message: err.message });
  }
});

SwaggerExpress.create(config, function(err, swaggerExpress) {
  if (err) { throw err; }

  // install middleware
  swaggerExpress.register(app);

  var port = process.env.PORT || 10010;
  app.listen(port);

  if (swaggerExpress.runner.swagger.paths['/hello']) {
    console.log('try this:\ncurl http://127.0.0.1:' + port + '/hello?name=Scott');
  }
});
