# Ошибка `avalonia.startDebugging not found`

Это означает, что команда расширения не зарегистрировалась в VS Code.
По одному сообщению нельзя определить, почему расширение не активировалось.

1. Открыть **папку репозитория**, где лежат `MoonlitWaters.sln` и `.vscode`.
2. В Extensions установить **C# Dev Kit** от Microsoft (он устанавливает C#)
   и **Avalonia for VSCode** от **Avalonia Team**.
3. Если Avalonia уже установлена: меню шестерёнки расширения → **Uninstall**,
   затем установить её снова. Проверить, что расширение включено для этого workspace.
4. `Ctrl+Shift+P` → **Developer: Reload Window**.
5. Открыть файл `.axaml` и дождаться завершения загрузки расширений.

Установка расширений из терминала, если команда `code` доступна:

```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension AvaloniaTeam.vscode-avalonia --force
```

Это отдельная настройка VS Code; команда обновления самого проекта — только
`git pull --ff-only`.

Для запуска проекта достаточно **F5 → Moonlit Waters — Avalonia**.
Добавленный `launch.json` использует стандартный C#-отладчик `coreclr`.
Этот запуск не обращается к команде `avalonia.startDebugging`.
Или запуск из терминала:

```bash
dotnet run --project MoonlitWaters/MoonlitWaters.csproj
```

Если команды расширения всё ещё не работают: **View → Output**, выбрать канал
Avalonia; также `Ctrl+Shift+P` → **Developer: Show Running Extensions**.
Проверить `dotnet --list-sdks`: нужен SDK 8 или новее. Пришли ошибку активации
из Output — по ней можно определить конкретную причину.

Документация:
- https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.vscode-avalonia
- https://code.visualstudio.com/docs/csharp/debugger-settings
