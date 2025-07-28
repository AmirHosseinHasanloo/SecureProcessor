# 📄 **مستندات فنی سیستم پردازش امن پیام**

## 📋 **معرفی کلی**

سیستم پردازش امن پیام یک پلتفرم مقیاس‌پذیر و مقاوم در برابر خطا برای پردازش پیام‌های ورودی با استفاده از الگوهای طراحی پیشرفته و معماری Event-Driven است. این سیستم از سه سرویس اصلی تشکیل شده است که با الگوهای طراحی مدرن و بهترین شیوه‌های توسعه نرم‌افزار پیاده‌سازی شده‌اند.

---

## 🏗️ **معماری سیستم**

### **ساختار پروژه**
```
SecureProcessorSolution/
│
├── SecureProcessor.Manager.Api        # سرویس مدیریت و سلامت‌سنجی
├── SecureProcessor.Dispatcher         # سرویس توزیع و مدیریت پیام‌ها
├── SecureProcessor.Processor          # سرویس پردازش‌کننده پیام‌ها
├── SecureProcessor.Shared             # مدل‌ها و ابزارهای مشترک
├── SecureProcessor.Core               # الگوهای طراحی و منطق کسب‌وکار
└── SecureProcessor.Tests              # تست‌های واحد و یکپارچه‌سازی
```

---

## 🎯 **اجزای اصلی سیستم**

### **1. سرویس مدیریت (Manager Service)**

#### **شرح کلی:**
سرویس مدیریت نقش مرکز تصمیم‌گیری و کنترل سیستم را بر عهده دارد. این سرویس وظیفه دریافت درخواست‌های سلامت‌سنجی، تصمیم‌گیری در مورد ظرفیت سیستم و مدیریت منابع را دارد.

#### **ویژگی‌های کلیدی:**
- **API سلامت‌سنجی**: `/api/module/health`
- **API پردازش پیام**: `/api/module/process-request`
- **مدیریت ظرفیت**: کنترل تعداد پردازش‌کننده‌های فعال
- **مدیریت بار**: نظارت بر وضعیت سیستم و جلوگیری از overload

#### **استدلال طراحی:**
> **چرا Manager فقط تصمیم‌گیری می‌کند؟**  
> اصل جداسازی مسئولیت‌ها (Separation of Concerns) - Manager تصمیم‌گیری می‌کند، Dispatcher اجرا می‌کند. این باعث افزایش قابلیت نگهداری، تست‌پذیری و مقیاس‌پذیری می‌شود. همچنین امکان داشتن چندین Dispatcher با یک Manager مرکزی فراهم می‌شود.

#### **کد کلیدی:**
```csharp
// ModuleController.cs
[HttpPost("health")]
public async Task<IActionResult> HealthCheck([FromBody] HealthCheckRequestModel request)
{
    // مقداردهی خودکار زمان سیستم
    var grpcRequest = new HealthCheckRequest
    {
        Id = request.Id,
        SystemTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // زمان فعلی سرور
        NumberOfConnectedClients = request.NumberOfConnectedClients
    };
    
    // فوروارد به Dispatcher از طریق gRPC
    var response = await client.HealthCheckAsync(grpcRequest);
}
```

---

### **2. سرویس توزیع‌کننده (Dispatcher Service)**

#### **شرح کلی:**
سرویس توزیع‌کننده قلب تپنده سیستم است که وظیفه مدیریت اتصال پردازش‌کننده‌ها، توزیع پیام‌ها و مدیریت صف را بر عهده دارد. این سرویس از الگوهای طراحی پیشرفته برای اطمینان از عملکرد بهینه استفاده می‌کند.

#### **ویژگی‌های کلیدی:**
- **gRPC Server**: ارتباط دوطرفه با پردازش‌کننده‌ها
- **مدیریت صف داخلی**: ترکیب پیام‌های تصادفی و خارجی
- **سلامت‌سنجی دوره‌ای**: ارتباط با Manager برای دریافت سیاست‌ها
- **مدیریت پردازش‌کننده‌ها**: ثبت، فعال‌سازی و غیرفعال‌سازی

#### **استدلال طراحی:**
> **چرا از صف داخلی استفاده می‌کنیم؟**  
> برای یکپارچه‌سازی پیام‌های خارجی و تصادفی - هر دو نوع پیام مسیر یکسانی را طی می‌کنند و اولویت پیام‌های خارجی دارند. این باعث می‌شود سیستم انعطاف‌پذیر و قابل گسترش باشد.

#### **کد کلیدی:**
```csharp
// MessageQueueService.cs
public async Task<Message> GetMessageAsync()
{
    // اولویت اول: پیام‌های خارجی
    if (MessageDispatcherServiceImpl.TryDequeueExternalMessage(out var externalMessage))
    {
        return externalMessage.Message;
    }
    
    // اولویت دوم: پیام‌های تصادفی
    return GenerateRandomMessage();
}

// MessageDispatcherServiceImpl.cs
public override async Task<ExternalMessageResponse> SubmitExternalMessage(
    ExternalMessageRequest request, ServerCallContext context)
{
    // اضافه کردن پیام خارجی به صف داخلی
    lock (_queueLock)
    {
        ExternalMessages.Enqueue(wrapper);
    }
}
```

---

### **3. سرویس پردازش‌کننده (Processor Service)**

#### **شرح کلی:**
سرویس پردازش‌کننده واحدهای اجرایی سیستم هستند که وظیفه دریافت پیام‌ها، پردازش آنها و ارسال نتایج را بر عهده دارند. این سرویس‌ها به صورت مستقل قابل مقیاس‌سازی هستند.

#### **ویژگی‌های کلیدی:**
- **Bidirectional Streaming**: ارتباط دوطرفه مداوم با Dispatcher
- **Self-Identification**: معرفی خودکار با شناسه منحصر به فرد
- **Continuous Processing**: پردازش مداوم پیام‌ها
- **Fault Recovery**: بازیابی خودکار پس از قطعی شبکه

#### **استدلال طراحی:**
> **چرا از gRPC Streaming استفاده می‌کنیم؟**  
> برای کاهش latency و افزایش throughput - پردازش‌کننده نیازی به درخواست مکرر ندارد و Dispatcher می‌تواند فوراً پیام ارسال کند. این باعث افزایش کارایی و کاهش مصرف منابع می‌شود.

#### **کد کلیدی:**
```csharp
// ProcessorClientService.cs
public override async Task Connect(IAsyncStreamReader<ProcessorMessage> requestStream,
    IServerStreamWriter<DispatcherMessage> responseStream,
    ServerCallContext context)
{
    // ارسال معرفی به Dispatcher
    var introduction = new ProcessorMessage
    {
        Introduction = new Introduction
        {
            Id = _processorId,
            Type = "RegexEngine"
        }
    };
    
    await requestStream.WriteAsync(introduction);
    
    // دریافت و پردازش پیام‌ها
    await foreach (var message in responseStream.ReadAllAsync())
    {
        // پردازش پیام
    }
}
```

---

## 🛡️ **الگوهای طراحی پیاده‌سازی شده**

### **1. الگوی Circuit Breaker**

#### **شرح:**
الگوی Circuit Breaker برای جلوگیری از فروپاشی زنجیرهایی (Cascading Failure) در صورت خرابی سرویس‌های راه دور استفاده می‌شود.

#### **عملکرد:**
```csharp
// CircuitBreaker.cs
public enum CircuitState
{
    Closed,    // مدار بسته - عملیات عادی
    Open,      // مدار باز - عملیات متوقف
    HalfOpen   // نیمه باز - تست مجدد
}

public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
{
    if (_state == CircuitState.Open)
    {
        if (DateTime.UtcNow - _lastFailureTime < _retryPeriod)
        {
            throw new CircuitBreakerOpenException("Circuit breaker is open");
        }
        _state = CircuitState.HalfOpen;
    }
    
    try
    {
        var result = await operation();
        OnSuccess(); // موفقیت - بستن مدار
        return result;
    }
    catch (Exception ex)
    {
        OnFailure(ex); // شکست - افزایش شمارنده خطا
        throw;
    }
}
```

#### **استدلال:**
> **چرا Circuit Breaker؟**  
> اگر سرویس Manager خراب شود، سرویس Dispatcher بارها و بارها تلاش نمی‌کند تا به آن متصل شود که باعث مصرف بیش از حد منابع، تاخیر در سیستم و احتمالاً فروپاشی سایر سرویس‌ها می‌شود. Circuit Breaker این مشکل را با متوقف کردن تلاش‌های ناموفق حل می‌کند.

---

### **2. الگوی Proxy**

#### **شرح:**
الگوی Proxy یک لایه واسط بین کلاینت و سرویس واقعی ایجاد می‌کند که وظیفه مدیریت فراخوانی‌های راه دور، retry logic و مدیریت خطا را بر عهده دارد.

#### **عملکرد:**
```csharp
// ServiceProxy.cs
public class ServiceProxy<T> : IServiceProxy<T> where T : class
{
    public async Task<TResult> InvokeAsync<TResult>(
        Func<T, Task<TResult>> operation, 
        int retryCount = 3, 
        TimeSpan retryDelay = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    return await operation(_service);
                }
                catch (Exception ex) when (attempt < retryCount)
                {
                    await Task.Delay(retryDelay);
                }
            }
            throw new InvalidOperationException("All retry attempts failed");
        });
    }
}
```

#### **استدلال:**
> **چرا Proxy؟**  
> برای جداسازی منطق شبکه از منطق کسب‌وکار - کد اصلی ساده و خوانا می‌ماند و منطق پیچیده retry و مدیریت خطا در یک جا مدیریت می‌شود. این باعث افزایش قابلیت تست‌پذیری و نگهداری می‌شود.

---

### **3. معماری Event-Driven**

#### **شرح:**
معماری Event-Driven بر اساس تولید، تشخیص و واکنش به رویدادها کار می‌کند. این معماری ارتباط لحظه‌ای و غیرهمزمان بین سرویس‌ها را فراهم می‌کند.

#### **عملکرد:**
```csharp
// MessageDispatcherService.cs
public override async Task Connect(IAsyncStreamReader<ProcessorMessage> requestStream,
    IServerStreamWriter<DispatcherMessage> responseStream,
    ServerCallContext context)
{
    await foreach (var message in requestStream.ReadAllAsync())
    {
        switch (message.ContentCase)
        {
            case ProcessorMessage.ContentOneofCase.Introduction:
                await HandleIntroductionAsync(message.Introduction, responseStream);
                break;
            case ProcessorMessage.ContentOneofCase.Result:
                await HandleResultAsync(message.Result);
                break;
        }
    }
}
```

#### **استدلال:**
> **چرا Event-Driven؟**  
> برای loose coupling و مقیاس‌پذیری - سرویس‌ها به هم وابسته نیستند و می‌توان پردازش‌کننده‌های بیشتری اضافه کرد بدون اینکه نیاز به تغییر در سایر سرویس‌ها باشد. این باعث افزایش انعطاف‌پذیری و قابلیت توسعه می‌شود.

---

## 📊 **مدیریت بار و مقیاس‌پذیری**

### **سیاست‌های مدیریت بار:**
- **حداکثر پردازش‌کننده‌های فعال**: 5 عدد
- **حداکثر کلاینت‌های متصل**: 20 عدد  
- **انقضای دسترسی**: 10 دقیقه پس از هر سلامت‌سنجی

### **الگوریتم مدیریت بار:**
```csharp
// در HealthCheck سرویس Manager
var response = new HealthCheckResponse
{
    IsEnabled = true,
    NumberOfActiveClients = Math.Min(request.NumberOfConnectedClients, 5), // حداکثر 5 فعال
    ExpirationTime = DateTime.UtcNow.AddMinutes(10)
};
```

#### **استدلال:**
> **چرا محدودیت‌های مشخص؟**  
> برای جلوگیری از overload و اطمینان از عملکرد بهینه سیستم - مدیریت منابع و پیشگیری از crash شدن سیستم. این محدودیت‌ها قابل پیکربندی هستند و می‌توانند بر اساس نیاز تغییر کنند.

---

## 🔧 **مدیریت خطا و بازیابی**

### **استراتژی‌های مدیریت خطا:**
- **Retry Logic**: 5 تلاش با فاصله 10 ثانیه
- **Circuit Breaker**: جلوگیری از تلاش‌های مکرر بی‌فایده
- **Reconnection**: اتصال مجدد خودکار پس از حل مشکل
- **Logging**: لاگ‌نویسی جامع از تمام عملیات

### **کد مدیریت خطا:**
```csharp
// در HealthCheckService
public async Task<HealthCheckResponse> CheckHealthAsync(string managerUrl)
{
    return await _serviceProxy.InvokeAsync(async service =>
    {
        var response = await _httpClient.PostAsJsonAsync($"{managerUrl}/api/module/health", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
        }
        throw new HttpRequestException($"Health check failed: {response.StatusCode}");
    }, retryCount: 5, retryDelay: TimeSpan.FromSeconds(10));
}
```

#### **استدلال:**
> **چرا این مکانیزم‌ها؟**  
> برای افزایش قابلیت اطمینان سیستم - حتی در صورت بروز خطا، سیستم قادر به بازیابی و ادامه کار است. این باعث افزایش uptime و کاهش نیاز به مداخله دستی می‌شود.

---

## 🧪 **تست‌پذیری و کیفیت کد**

### **استراتژی تست:**
- **Unit Tests**: تست‌های واحد برای هر کامپوننت
- **Integration Tests**: تست‌های یکپارچه‌سازی بین سرویس‌ها
- **Mock Objects**: استفاده از Mock برای وابستگی‌های خارجی
- **Code Coverage**: پوشش تست بالا برای اطمینان از کیفیت

### **مثال تست:**
```csharp
[Fact]
public async Task ExecuteAsync_Should_Open_Circuit_After_Failure_Threshold_Exceeded()
{
    // Arrange
    Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
    await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
    await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
    
    Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());
}
```

#### **استدلال:**
> **چرا تست‌های جامع؟**  
> برای اطمینان از عملکرد صحیح سیستم و جلوگیری از regression - تست‌های خودکار اطمینان می‌دهند که تغییرات جدید باعث شکستن قابلیت‌های موجود نمی‌شوند.

---

## 🎯 **مزایای معماری پیاده‌سازی شده**

### **مقایسه با سیستم ساده:**
```
سیستم ساده:
❌ بدون Fault Tolerance → خرابی کامل
❌ بدون Load Balancing → بار نامتوازن  
❌ بدون Scalability → مقیاس‌پذیری پایین
❌ بدون Monitoring → دشواری در عیب‌یابی

سیستم پیشرفته:
✅ Fault Tolerance → بازیابی خودکار
✅ Load Balancing → توزیع عادلانه بار
✅ Scalability → افزودن پردازش‌کننده بیشتر
✅ Monitoring → لاگ‌نویسی جامع
```

### **در عمل:**
```
اگر Manager 5 دقیقه خراب باشد:
سیستم ساده: 1000 تلاش ناموفق = خرابی کامل
سیستم پیشرفته: Circuit Breaker فعال → سیستم سالم می‌ماند
```

---

## 📈 **عملکرد و قابلیت‌های سیستم**

### **مقیاس‌پذیری:**
- امکان افزودن پردازش‌کننده بیشتر بدون تغییر کد
- مدیریت هوشمند ظرفیت با HealthCheck
- مقاومت در برابر افزایش ناگهانی بار

### **تحمل خطا:**
- مدیریت قطعی شبکه با Retry Logic
- جلوگیری از فروپاشی زنجیرهایی با Circuit Breaker
- بازیابی خودکار پس از حل مشکل

### **مانیتورینگ:**
- لاگ‌نویسی جامع از تمام عملیات
- ردیابی وضعیت سیستم و پردازش‌کننده‌ها
- مانیتورینگ بار سیستم و عملکرد

## ⚙️ ** نحوه اجرای پروژه 

ابتدا برای اجرای پروژه یک کانفیگ جدید برای اجرا بسازید که به ترتیب Manager.API و Dispatcher و Processor را اجرا کند . 
--نکته : حتما پروژه API رو به روی https قرار دهید برای اجرا.
موفق و پیروز باشید.
