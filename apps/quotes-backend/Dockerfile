FROM node:12-alpine
RUN mkdir -p /usr/src/app
COPY ./* /usr/src/app/
WORKDIR /usr/src/app
RUN npm install
EXPOSE 3000
CMD [ "node", "index.js" ]
