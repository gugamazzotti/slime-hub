# Slime Hub

Pote de slimes em **retrato** para WebGL, feito em código para **Unity 2022.3 LTS** (Built-in Render Pipeline). Dois slimes do mesmo tamanho se juntam e sobem de nível. O maior par entra no hub e some, liberando o pote. Se a pilha passar da linha vermelha, transborda.

No celular o canvas ocupa a tela. No desktop a página mantém um quadro ~9:16 no centro.

## Como jogar

| Ação | Toque | Teclado |
|---|---|---|
| Mirar | Arrastar, ou mover o mouse | `A`/`D` ou `←`/`→` |
| Soltar o slime | Soltar o dedo, ou clicar | `Espaço` ou `Enter` |
| Começar / tentar de novo | Toque | `Espaço` ou `Enter` |

A cor da direita é a próxima. O recorde fica salvo no navegador.

## Como abrir

1. Unity Hub → **Add project from disk** → esta pasta. O projeto está em **2022.3.20f1**.
2. Abra `Assets/Scenes/Main.unity` e dê **Play**. A cena é vazia de propósito: `SlimeGame` monta câmera, pote, física e UI em runtime.
3. No Game view, use uma resolução alta em retrato, por exemplo **1080×1920**.

## Build WebGL

O material do jogo usa o shader `SlimeHub/Blob`, sem keywords. Ele está em `Resources` e em *Always Included Shaders*. O player não puxa o Standard, então o build não dispara a explosão de variantes e os slimes não caem no magenta.

- Menu **Slime Hub → Build WebGL** (Gzip com *decompression fallback*, template retrato, canvas **540×960**). A saída fica em `Build/WebGL`.
- Linha de comando:

```
Unity -batchmode -quit -projectPath . -executeMethod SlimeHub.EditorTools.WebGLBuilder.Build -buildTarget WebGL
```

Teste local com `npx http-server Build/WebGL`. Não abra o `index.html` por `file://`.

## Preview

O workflow `.github/workflows/webgl-preview.yml` roda no GameCI a cada push em `daily/**`. O job tem limite de **60 minutos**. Ele publica só em

`https://gugamazzotti.github.io/slime-hub/preview/<branch>/`

e não substitui a raiz do GitHub Pages. O repositório precisa dos secrets `UNITY_LICENSE`, `UNITY_EMAIL` e `UNITY_PASSWORD`.
