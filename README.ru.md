# AdvancedPlanting

![Version](https://img.shields.io/badge/version-0.4.3-ffcc00?style=for-the-badge&labelColor=555555) ![Valheim](https://img.shields.io/badge/valheim-1.0.17-f07c34?style=for-the-badge&labelColor=555555) ![Platform](https://img.shields.io/badge/platform-client-0a7bc2?style=for-the-badge&labelColor=555555) ![Languages](https://img.shields.io/badge/languages-EN%20%7C%20RU%20%7C%20UA%20%7C%20DE%20%7C%20FR%20%7C%20ES%20%7C%20PT%20%7C%20PL%20%7C%20ZH%20%7C%20JA-2ea44f?style=for-the-badge&labelColor=555555) [![Donate](https://img.shields.io/badge/boosty-donate-f15f2c?style=for-the-badge&logo=boosty&logoColor=white&labelColor=555555)](https://boosty.to/kepqa/donate)

Сделано **kepqa** с помощью **Claude Code**.

Посадка вдоль линий, дуг, окружностей, кривых и залитых контуров.

![Посаженный многорядный круг](images/preview-1-planted-circle.jpg)

## Возможности

- Пять типов пути: линия, дуга, окружность, кривая, контур.
- Параллельные ряды, заливка замкнутых фигур до центра.
- Разброс и прореживание для естественного вида.
- Предпросмотр: призраки и кольца радиуса роста до посадки.
- Ванильная стоимость: семена, выносливость, прочность, навык.
- Частичная посадка при нехватке выносливости, автодосадка по желанию.
- Только клиент.
- Несовместим с другими модами на посадку.

![Круг, 4 ряда, пустая середина](images/preview-2-circle-hollow.jpg)

![Кривая, 3 ряда](images/preview-3-curve.jpg)

![Две вложенные кривые](images/preview-5-two-curves.jpg)

## Управление

| Действие | Клавиша |
|---|---|
| Точка / посадка | ЛКМ |
| Завершить кривую / замкнуть контур | `Z` или клик по последней / первой точке |
| Ряды | Колесо мыши |
| Заливка вкл/выкл | `G` |
| Сбросить фигуру | `Backspace` |
| Меню посадки | `B` |

Клавиши настраиваются в `BepInEx/config/kepqa.AdvancedPlanting.cfg`.

![Меню посадки](images/preview-6-settings-wheel.jpg)

![Окно настроек](images/preview-4-settings-window.jpg)

## Перевод

- Файлы: `Translations/<Язык>.txt`, формат `ключ = значение`.
- В комплекте: английский, русский, украинский, немецкий, французский, испанский, португальский (Бразилия), польский, китайский (упрощённый), японский.
- Новый язык: скопировать `English.txt` под названием языка из настроек игры.
