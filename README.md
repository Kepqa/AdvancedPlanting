# AdvancedPlanting

Мод для Valheim (BepInEx 5), который сажает растения вдоль произвольных кривых: отрезок, дуга, окружность, сплайн через любые точки и заливка замкнутого контура. Любая кривая превращается в изогнутую сетку из параллельных рядов. Есть регулируемая хаотичность разброса и прореживание, чтобы посадки выглядели естественно, а не как линейка.

Отличие от PlantEasily: фигура задаётся контрольными точками, зафиксированными в мире, а не прямоугольником вокруг курсора. Вы ставите точки, ходите вокруг, крутите параметры и видите все будущие растения призраками до того, как потратить хоть одно семя.

## Управление

Работает только в режиме строительства с семенем или саженцем в руках. Все клавиши меняются в `BepInEx/config/kepqa.AdvancedPlanting.cfg`.

| Действие | По умолчанию |
|---|---|
| Поставить контрольную точку под курсором | `F` |
| Убрать последнюю точку | `Z` |
| Сбросить фигуру | `X` |
| Сменить тип пути: линия, дуга, окружность, кривая, заливка | `G` |
| Сторона рядов: обе, слева, справа | `H` |
| Перебросить случайность | `R` |
| Посадить всю фигуру | ЛКМ, как обычная посадка |
| Шаг между растениями | `Alt` + колесо |
| Число рядов (для заливки 0 = до центра) | `Ctrl` + колесо |
| Хаотичность 0..1 | `Shift` + колесо |
| Прореживание 0..0.9 | `Shift` + `Alt` + колесо |
| Включить/выключить мод | `F7` |

Пока точек меньше, чем нужно режиму, мод не вмешивается, и посадка работает как в ванили. Нужное число точек: линия 2, дуга и окружность 3, кривая от 2, заливка от 3.

Шаг по умолчанию равен удвоенному радиусу роста растения. Минимальная дистанция между растениями (радиус роста плюс запас) соблюдается всегда, разброс никогда её не нарушает, поэтому растения не глушат друг друга. Красные призраки показывают точки, где сажать нельзя: не вскопано, чужой биом, вода, занято, чужой вард. Они пропускаются при посадке.

## Сборка

Нужны .NET SDK 8 и установленный Valheim с BepInEx 5. Игровые DLL берутся прямо из папки игры, в репозитории их нет.

1. Создайте `src/AdvancedPlanting/Local.props`:
   ```xml
   <Project>
     <PropertyGroup>
       <ValheimPath>D:\Steam\steamapps\common\Valheim</ValheimPath>
       <CopyToGame>true</CopyToGame>
     </PropertyGroup>
   </Project>
   ```
   Вместо файла можно задать переменную окружения `VALHEIM_INSTALL`.
2. `dotnet build src/AdvancedPlanting -c Release`

С `CopyToGame=true` обе DLL (`AdvancedPlanting.dll` и `AdvancedPlanting.Core.dll`) копируются в `BepInEx/plugins/AdvancedPlanting` после каждой сборки. Приватные члены игры открываются через `BepInEx.AssemblyPublicizer.MSBuild` во время сборки.

Проверки без игры:

```
dotnet test tests/AdvancedPlanting.Core.Tests        # геометрия
dotnet build tests/AdvancedPlanting.StubBuild        # плагин на заглушках API
dotnet run --project tools/LayoutPreview -- out      # SVG-превью раскладок
```

Проект-заглушка ловит только ошибки в нашем коде. Настоящая проверка имён методов игры происходит при сборке против `assembly_valheim.dll`.

## Как устроено

- `src/AdvancedPlanting.Core` — чистая геометрия без зависимостей от Unity: кривые, эквидистанты рядов с обрезкой на вогнутой стороне, детерминированный шум, фильтр минимальной дистанции. Покрыта тестами.
- `src/AdvancedPlanting` — плагин: конфиг, состояние фигуры, проверка точек по правилам игры, пул призраков, отрисовка кривых, HUD, патчи Harmony.
- Патчи: `Player.UpdatePlacementGhost` (обновление превью), `Player.PlacePiece` (посадка всей фигуры вместо одной точки), `Player.UpdatePlacement` (перехват колеса с модификаторами).

Посадка идёт через ванильный `PlacePiece` для каждой точки, поэтому ресурсы списываются как обычно, а сервер и другие игроки видят растения без специальной синхронизации. Мод нужен только тому, кто сажает.

## Статус

Первая версия, в игре ещё не проверена. Сигнатуры игровых методов подобраны по памяти о недавних версиях, `PlacePiece` ищется через рефлексию и поддерживает обе известные формы. Первое, что стоит сделать после сборки: зайти в мир, взять морковку, поставить две точки и посмотреть лог BepInEx.

---

## English

Valheim mod (BepInEx 5) that plants along arbitrary curves: line, arc, circle, free spline and closed-shape fill. Any curve becomes a bent grid of parallel rows. Adjustable jitter and thinning make plantings look natural. Control points are fixed in the world, the preview shows every plant as a ghost before you spend a seed, and every plant goes through the vanilla `PlacePiece`, so resources and multiplayer sync work as usual. Build instructions are above: create `Local.props` with your `ValheimPath`, then `dotnet build src/AdvancedPlanting -c Release`.
