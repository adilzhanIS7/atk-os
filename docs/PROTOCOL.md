# Протокол мышей ATK на чипе Compx (черновик)

Источник: анализ ATK V HUB 2.3.59 (`dist/index-*.js`, класс `Compx`).
Здесь только описание протокола — код оригинала не копируем.

## Устройства

| Режим        | VID      | PID      | Профиль            |
|--------------|----------|----------|--------------------|
| Провод       | `0x373B` | `0x1084` | compx-3311         |
| 2.4G ресивер | `0x373B` | `0x1085` | compx-3311  ← мой  |
| 2.4G ресивер | `0x3554` | `0xFB12` | compx-3311         |

WebHID-фильтр оригинала: usagePage `0xFF04`, usage `0x02`. Но это только выбор интерфейса.
**На Windows сам канал команд — соседняя коллекция `MI_01&COL05`, usage `0xFF02:0x02`,
input/output report ID 8 длиной 17 байт.** (`COL06` = `0xFF04` содержит лишь feature report 6.)
Ищем коллекцию по наличию output report 8.

Проверено на железе: VXE R1 SE+ (CID 2, MID 31), ресивер `373B:1085`, прошивка 0300.

В приложении ~200 VID/PID для разных моделей ATK/VXE/VGN — таблица ещё не выгружена.

## Формат пакета

Output report, **Report ID = 8**, payload 16 байт:

| Байт  | Поле                                             |
|-------|--------------------------------------------------|
| 0     | commandId                                        |
| 1     | commandStatus (в ответе; 0 = OK)                 |
| 2..3  | eepromAddress, big-endian                        |
| 4     | dataValidLen                                     |
| 5..14 | data (до 10 байт)                                |
| 15    | checksum = `(0x55 - (8 + sum(bytes[0..14]))) & 0xFF` |

Ответ приходит input report'ом; ждём пакет, у которого byte 0 совпадает с commandId.
byte 0 = `0x0A` — спонтанное уведомление о смене состояния (`ReportMouseStatus`).

Многие значения в EEPROM хранятся парой «значение + проверка»: `value`, затем `0x55 - value`.

## Команды

| ID | Имя | ID | Имя |
|----|-----|----|-----|
| 1  | DownLoadData (рукопожатие) | 14 | GetCurrentConfig (профиль) |
| 2  | DownLoadDriverStatus | 15 | SetCurrentConfig |
| 3  | GetWirelessMouseOnline | 16 | GetMouseCIDMID |
| 4  | GetBatteryLevel | 18 | GetMouseVersion |
| 5  | SetWirelessDonglePair | 19 | DongleExitPair |
| 6  | GetWirelessDonglePairResult | 20/21 | Set/Get 4K RGB mode |
| 7  | **SetEEPROM** | 22/23 | Set/Get FarDistance mode |
| 8  | **GetEEPROM** | 24/25 | Set/Get Dongle light |
| 9  | RestoreFactory | 90/91 | статусы прошивки (не трогаем) |
| 10 | ReportMouseStatus (событие) | 13 | EnterUSBUpgradeMode (**опасно**) |

Батарея (cmd 4): data[0] = уровень %, data[1] = зарядка, data[2..3] = напряжение (BE).

## Адреса EEPROM

| Адрес | Что |
|-------|-----|
| 0..5  | reportRate, maxDpi, currentDpi (каждое + CRC-байт) |
| 10    | silentHeight (LOD) |
| 12/20/28/36 | DPI 1–8 (по 2 уровня на запрос) |
| 44/52/60/68 | цвета DPI 1–8 |
| 76..83 | эффекты подсветки DPI |
| 84..95 | подсветка («article lamp») |
| 96..156 | кнопки key0..key15 (по 4 байта) |
| 169 | stabilizationTime (debounce) |
| 171 | motionSync |
| 173 | closeLedTime |
| 175 | linearCorrection (angle snapping) |
| 177 | rippleControl |
| 181..186 | sensorEnable / sensorTime / sensorMode |
| 187 | rfTxTime |
| 256+ | шорткаты, дальше макросы |

Частота опроса (reportRate): 1=1000, 2=500, 4=250, 8=125, 16=2000, 32=4000, 64=8000 Гц.

## Подключение (как делает оригинал)
1. cmd 3 — мышь онлайн? (data[0] == 1). Для ресивера без включённой мыши дальше не идём.
2. cmd 1 — рукопожатие: 4 случайных байта, в ответе data[4]=CID, data[5]=MID, data[6]=тип.
3. Чтение настроек через GetEEPROM (cmd 8).

## DPI
Слот = 4 байта: `xLow, yLow, ext, check` (check = 0x55 - (x+y+ext)). Модель берётся по CID/MID
(`SensorTable`), от неё зависит кодирование:
- **Table** (макс. ≤ 18000: R1 SE/SE+, R1 S+, Star R1): xLow → DPI по таблице `DpiTable`
  (нелинейная, 120 значений до 10000). Флаг ext `0x20` = удвоить (10050–20000).
- **Step50** (большинство, макс. 26000): `raw = xLow | ((ext&0x0C)>>2)<<8`, dpi = 50·(raw+1); ext&0x11 → ×2.
- **Step10** («ultimate»: F1/X1/Z1 Ultra и т.п.): ext&0x02 → 50·raw+10050, иначе 10·(raw+1).

Кнопки (4 байта): class, value1, value2, check. class: 0 откл, 1 мышь (1 L, 2 R, 4 M, 8 back, 16 fwd),
2 DPI (1 цикл, 2 +, 3 −), 3 горизонт. прокрутка, 4 «огонь» (интервал, кол-во), 5 шорткат, 6 макрос,
7 частота опроса, 8 подсветка («Chandeliers»), 9 профиль, 10 DPI lock, 11 колесо (1 вверх, 2 вниз).

## TODO
- Формат шорткатов и макросов (адреса 256+).
- Выгрузить полную таблицу устройств (VID/PID → контроллер/фичи).
- Проверить запись (SetEEPROM) — сначала на безопасном параметре (LOD или цвет DPI).
