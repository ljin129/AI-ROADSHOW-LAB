FROM harbor.betawm.com/node/basenode:v1.1 as build
WORKDIR /src
COPY . .
RUN npm config set registry https://nexus.betawm.com/repository/npm-group/
RUN npm i

FROM build AS publish
ARG buildenv=build
RUN npm run $buildenv

FROM nginx AS final
WORKDIR /beta
EXPOSE 80
EXPOSE 443
COPY --from=publish /src/dist .
ENTRYPOINT ["nginx","-g","daemon off;"]
