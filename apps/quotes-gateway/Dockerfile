FROM node:12
RUN groupadd -g 999 appuser && useradd -r -u 999 -g appuser appuser
ENV NODE_ENV production
# Enable chokidar polling so hot-reload mechanism can work on docker or network volumes
ENV CHOKIDAR_USEPOLLING true
WORKDIR /usr/src/app
COPY package*.json /usr/src/app/
RUN npm install
EXPOSE 3000 9876
USER appuser
COPY . /usr/src/app
CMD [ "node", "server.js" ]