FROM golang:1.23-alpine AS build
WORKDIR /src
ARG VERSION=dev
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build \
    -ldflags "-s -w -X main.version=${VERSION}" \
    -o /out/deathborn ./cmd/deathborn

FROM alpine:3.20
RUN apk add --no-cache wget
RUN adduser -D -u 10001 app
USER app
COPY --from=build /out/deathborn /usr/local/bin/deathborn
EXPOSE 8080
ENTRYPOINT ["deathborn"]
