# Bounan Downloader

## Tips

### Running telegram locally

```shell
docker run -it --rm -e "TELEGRAM_LOCAL" --env-file .env -p 25565:8081 aiogram/telegram-bot-api:latest
```

```shell
podman run -it --rm -e "TELEGRAM_LOCAL" --env-file .env -p 25565:8081 aiogram/telegram-bot-api:latest
```
