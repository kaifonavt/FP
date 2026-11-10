# ТЕХНИЧЕСКОЕ ЗАДАНИЕ (ТЗ)

## Разработка CLI-приложения 

---

## 1. Общее описание проекта

Программа представляет собой консольное (CLI) приложение на платформе **.NET 8.0+ (C#)**, выполняющее роль системы управления физической безопасностью, контролем доступа и обслуживанием помещений.

Приложение функционирует в интерактивном режиме с непрерывным циклом обработки ввода (REPL pattern):
$$\text{Start} \longrightarrow \text{User Input} \longrightarrow \text{Response} \longrightarrow \text{Waiting for Input}$$

---

## 2. Архитектурные требования и стек технологий

### 2.1. Технологический стек

* **Язык и платформа:** C# 12+, .NET 8.0+
* **Формат данных:** JSON (использование `System.Text.Json`)
* **Стиль программирования:** Преимущественно функциональный подход (Functional Programming Paradigm)

### 2.2. Требования к функциональному стилю

1. **Неизменяемость (Immutability):** Все доменные модели и состояния описываются с помощью неизменяемых типов (`record`).
2. **Обработка ошибок без Exception (Functional Error Handling):**
   * Использование шаблона `Result<T>` для ожидаемых ошибок.
   * Использование шаблона `Maybe<T>` вместо `null`.
   * Исключения (`try-catch`) допускаются только на самом верхнем уровне CLI-цикла для перехвата критических сбоев ввода-вывода.
3. **Функции высшего порядка (Higher-Order Functions, HOF):**
   * Все фильтрации, валидации и трансформации выполняются через передачи функций в качестве аргументов и LINQ.
4. **Каррирование (Currying) и частичное применение (Partial Application):**
   * Применение каррированных функций для конфигурации правил доступа, фильтров логов и предикатов безопасности.

---

## 3. Реестр Workflow (Командный интерфейс)

Система поддерживает 6 основных модулей (Workflow), вызываемых через текстовые команды в консоли:

```
sec-cli> <command-name> [arguments...]
```

### 3.1. Workflow 1: `scan-card` (Контроль доступа)

* **Команда:** `scan-card <card_id> <room_id>`
* **Назначение:** Эмуляция прикладывания карты к считывателю у двери/турникета.
* **Логика:**
  1. Cчитывание базы карт из JSON.
  2. Поиск карты по `card_id` с использованием `Result<Card, SecurityError>`.
  3. Валидация активности карты и статуса привязанного сотрудника.
  4. Проверка прав прохода в `room_id` через каррированную функцию проверки правил:
     $$\text{validateAccess} : \text{TimeSpan} \rightarrow \text{RoomLevel} \rightarrow \text{Employee} \rightarrow \text{Result}<\text{bool}, \text{SecurityError}>$$
  5. Регистрация события прохода/отказа в `audit.json`.

### 3.2. Workflow 2: `support-ticket` (Управление инцидентами)

* **Команды:**
  * `supporttickets list [--status <open|closed>]`
  * `supporttickets create <employee_id> <description>`
  * `supporttickets resolve <ticket_id>`
* **Назначение:** Учет заявок на ремонт замков, проблемы с картами и инциденты безопасности.

### 3.3. Workflow 3: `reservation`

* **Команды:**
  * `reservation book <room_id> <employee_id> <start_time> <end_time>`
  * `reservation cancel <reservation_id>`
  * `reservation list <room_id>`
* **Назначение:** Бронирование переговорных комнат и закрытых зон.
* **Логика:** Проверка пересечений интервалов времени с использованием предикатов высшего порядка.

### 3.4. Workflow 4: ``

* **Команды:**
  * ``
  * ``
  * ``
* **Назначение:** 

### 3.5. Workflow 5: ``

* **Команды:**
  * ``
* **Назначение:** 

### 3.6. Workflow 6: ``

* **Команды:**
  * ``
  * ``
* **Назначение:** Чтение и динамическое изменение системных параметров (например, режим работы здания, таймауты дверей).

---

## 4. Спецификация JSON-хранилища

Данные хранятся в файловой системе в формате JSON. Парсер обернут в чистые функции чтения/записи.

* `employees.json` — реестр сотрудников и их статусов.
* `cards.json` — реестр идентификационных карт и уровня допуска.
* `tickets.json` — список заявок техподдержки.
* `reservations.json` — журнал бронирования помещений.
* `config.json` — настройки приложения.

---

## 5. Пример реализации базовых функциональных типов (C#)

### 5.1. Тип Result

```csharp
public abstract record Result<T>
{
    private protected Result() { }
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(string Error) : Result<T>;
}
```
### 5.2. Тип Maybe

```csharp
public abstract record Maybe<T>
{
    private protected Maybe() { }
    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;
}
```
### 5.3. Тип Map

```csharp
public static Result<U> Map<T, U>(Result<T> r, Func<T, U> f) => r switch 
    {
        Result<T>.Ok(var x) => new Result<U>.Ok(f(x)),
        Result<T>.Fail(var e) => new Result<U>.Fail(e),
        _ => throw new ArgumentException("Unknown case")
    };
```

### 5.4. Тип Bind

```csharp
public static Result<U> Bind<T, U>(Result<T> r, Func<T, Result<U>> f) => r switch
    {
        Result<T>.Ok(var x) => f(x),
        Result<T>.Fail(var e) => new Result<U>.Fail(e),
        _ => throw new ArgumentException("Unknown case")
    };
```

### 5.5. Каррированная проверка доступа

```csharp
public static class AccessRules
{
    public static Func<TimeSpan, Func<int, Func<Employee, Result<bool, string>>>> CheckAccess =>
        currentTime => roomAccessLevel => employee =>
        {
            if (!employee.IsActive) 
                return new Result<bool, string>.Failure("Sotrudnik ne aktivien");

            if (employee.ClearanceLevel < roomAccessLevel) 
                return new Result<bool, string>.Failure("Nedostatochno prav dostupa");

            return new Result<bool, string>.Success(true);
        };
}
```
## 6. Требования к автоматизированному тестированию (Automated Testing)

Разработка приложения должна сопровождаться написанием автоматизированных тестов (xUnit / NUnit + FluentAssertions).

### 6.1. Модульное тестирование (Unit Tests)

Функциональные типы (Result, Maybe): Покрытие тестами функций Map, Bind, Match и свёртки состояний.

Чистые функции бизнес-логики: 100% покрытие unit-тестами функций каррированной проверки доступа (CheckAccess), валидации временных интервалов бронирования (reservation) и фильтрации логов (audit-log).

Парсеры и HOF: Проверка обработки предикатов высшего порядка без выполнения сайд-эффектов.

### 6.2. Интеграционное тестирование (Integration Tests)

JSON Data Repository: Тестирование чтения/записи JSON-файлов через временные изолированные файлы (in-memory file system или временные каталоги).

Workflow Pipeline Tests: Пошаговая эмуляция команд (scan-card, reservation book, supporttickets create) с проверкой итогового состояния JSON-хранилища и корректности ответа типа Result.

### 6.3. Свойства и инварианты (Property-Based Testing - Опционально / Желательно)

Использование библиотеки FsCheck для проверки инвариантов бизнес-логики (например, невозможность двойного бронирования помещения на один и тот же интервал времени при любых комбинациях случайных входных данных).