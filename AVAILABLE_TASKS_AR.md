# المهام المتاحة للمساهمين - دليل التظوير بالعربية

[🇬🇧 Read in English](./AVAILABLE_TASKS.md)

هذا المستند يوفر شرحاً تفصيلياً وفلسفة العمل لكل مهمة في قسم البرمجة وتطوير النقاط النهائية الخاصة بالخادم Backend.

---

## المرحلة 4: نقاط النهاية البرمجية المعالجة بواسطة MediatR و API Controllers

---

### المهمة 51: إنشاء نقطة دخول تسجيل الدخول
- **المواقع**:
  `src/Buy2.Application/Features/Authentication/Login/LoginCommand.cs`
  `src/Buy2.Api/Controllers/AuthLoginController.cs`
- **فلسفة العمل والمنطق الأمني**:
  عملية المصادقة هي البوابة الرئيسية للنظام. يجب على المستخدم إثبات هويته والحصول على رمز مميز مشفر.
- **خطوات التنفيذ**:
  1. تعريف أمر تسجيل الدخول:
     `LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>`
  2. في فئة المعالجة
     `LoginCommandHandler`
     - البحث عن الموظف باستخدام البريد الإلكتروني عبر
       `IRepository<Employee>`
     - التحقق من مطابقة كلمة المرور المشفرة.
     - إنشاء رمز توكن عبر
       `IJwtTokenGenerator`
     - إرجاع كائن
       `LoginResponseDto(token, employee)`
  3. في المتحكم
     `AuthLoginController`
     حقن الوسيط
     `ISender mediator`
     وإنشاء نقطة النهاية
     `POST api/v1/auth/login`

---

### المهمة 52: إنشاء نقطة إعادة ضبط كلمة المرور
- **المواقع**:
  `src/Buy2.Application/Features/Authentication/ResetPassword/ResetPasswordCommand.cs`
  `src/Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **فلسفة العمل والمنطق الأمني**:
  تتيح للمستخدم استعادة حسابه بأمان في حال نسيان كلمة المرور.
- **خطوات التنفيذ**:
  1. تعريف أمر إعادة الضبط:
     `ResetPasswordCommand(string Email, string NewPassword) : IRequest<bool>`
  2. في فئة المعالجة:
     - الاستعلام عن الحساب بالبريد الإلكتروني.
     - تشفير كلمة المرور الجديدة قبل التخزين.
     - حفظ التحديثات عبر
       `IUnitOfWork`
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/auth/password/reset`

---

### المهمة 53: إنشاء نقطة إضافة صلاحية جديدة
- **المواقع**:
  `src/Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
  `src/Buy2.Api/Controllers/CreateRoleController.cs`
- **فلسفة العمل والمنطق الأمني**:
  إدارة الصلاحيات الديناميكية لتحديد الأدوار الإدارية والتشغيلية في النظام.
- **خطوات التنفيذ**:
  1. تعريف أمر إنشاء الدور:
     `CreateRoleCommand(string RoleName, List<string> Permissions) : IRequest<int>`
  2. في فئة المعالجة:
     - التأكد من عدم تكرار اسم الدور.
     - تحويل قائمة الصلاحيات إلى نص
       `JSON`
     - حفظ الدور الجديد وإرجاع المعرف الخاص به.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/roles`

---

### المهمة 54: إنشاء نقطة الحذف الإداري للدور
- **المواقع**:
  `src/Buy2.Application/Features/Roles/DeleteRole/DeleteRoleCommand.cs`
  `src/Buy2.Api/Controllers/DeleteRoleController.cs`
- **فلسفة العمل والمنطق الأمني**:
  الحذف الناعم يحافظ على سجلات المراجعة ويمنع كسر البيانات المرتبطة بالموظفين.
- **خطوات التنفيذ**:
  1. تعريف أمر الحذف:
     `DeleteRoleCommand(int RoleId) : IRequest<bool>`
  2. في فئة المعالجة:
     - جلب الدور والتأكد من عدم وجود موظفين نشطين مرتبطي به.
     - تغيير حالة الدور إلى غير نشط ثم الحفظ عبر
       `IUnitOfWork`
  3. في المتحكم:
     إضافة نقطة النهاية
     `DELETE api/v1/roles/{id}`

---

### المهمة 55: إنشاء نقطة تسجيل موظف جديد
- **المواقع**:
  `src/Buy2.Application/Features/Employees/OnboardEmployee/OnboardEmployeeCommand.cs`
  `src/Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **فلسفة العمل والمنطق الأمني**:
  إضافة الموظفين الجدد إلى النظام مع ربطهم بالموقع والمسمى الوظيفي.
- **خطوات التنفيذ**:
  1. تعريف أمر الإضافة:
     `OnboardEmployeeCommand(string FirstName, string LastName, string Email, int JobRoleId, int SiteId) : IRequest<int>`
  2. في فئة المعالجة:
     - التحقق من عدم تكرار البريد الإلكتروني.
     - التحقق من وجود الوظيفة والموقع المفترصين.
     - إنشاء الموظف وحفظ السجل.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/employees/onboard`

---

### المهمة 56: إنشاء نقطة رفع مستندات الموظف
- **المواقع**:
  `src/Buy2.Application/Features/Employees/UploadDocument/UploadEmployeeDocumentCommand.cs`
  `src/Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **فلسفة العمل والمنطق الأمني**:
  تخزين روابط العقود والهويات الرسمية للموظفين لضمان الامتثال القانوني.
- **خطوات التنفيذ**:
  1. تعريف أمر الرفع:
     `UploadEmployeeDocumentCommand(int EmployeeId, string Category, string StorageUrl) : IRequest<int>`
  2. في فئة المعالجة:
     - التحقق من وجود الموظف المستهدف.
     - حفظ بيانات المستند ورابط السحابة.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/employees/{id}/documents`

---

### المهمة 57: إنشاء نقطة تسجيل المخالفات الإدارية
- **المواقع**:
  `src/Buy2.Application/Features/Employees/LogViolation/LogDisciplinaryViolationCommand.cs`
  `src/Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **فلسفة العمل والمنطق الأمني**:
  توثيق المخالفات والإنذارات الميدانية لاحتساب النقاط والجزاءات.
- **خطوات التنفيذ**:
  1. تعريف أمر المخالفة:
     `LogDisciplinaryViolationCommand(int EmployeeId, string Severity, string Description) : IRequest<int>`
  2. في فئة المعالجة:
     - إنشاء سجل المخالفة وربطه بالموظف وحفظ التغييرات.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/employees/{id}/violations`

---

### المهمة 58: إنشاء نقطة إضافة موقع فرع جديد
- **المواقع**:
  `src/Buy2.Application/Features/Sites/CreateSite/CreateSiteCommand.cs`
  `src/Buy2.Api/Controllers/CreateSiteController.cs`
- **فلسفة العمل والمنطق الأمني**:
  تحديد الإحداثيات الجغرافية وعناوين الشبكات لإثبات حضور الموظفين عبر الهاتف.
- **خطوات التنفيذ**:
  1. تعريف أمر الموقع:
     `CreateSiteCommand(string SiteName, decimal Latitude, decimal Longitude, List<string> MacWhitelist) : IRequest<int>`
  2. في فئة المعالجة:
     - حفظ خطوط الطول والعرض وتشفير شبكات الواي فاي.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/sites`

---

### المهمة 59: إنشاء نقطة عرض الفروع المتاحة
- **المواقع**:
  `src/Buy2.Application/Features/Sites/GetSites/GetSitesQuery.cs`
  `src/Buy2.Api/Controllers/GetSitesController.cs`
- **فلسفة العمل والمنطق الأمني**:
  عرض الفروع للم مدراء لاختيار موقع الجدول الزمني والمناوبات.
- **خطوات التنفيذ**:
  1. تعريف استعلام الفروع:
     `GetSitesQuery() : IRequest<List<SiteDto>>`
  2. في فئة المعالجة:
     - جلب الفروع النشطة وتحويلها إلى قائمة
       `SiteDto`
  3. في المتحكم:
     إضافة نقطة النهاية
     `GET api/v1/sites`

---

### المهمة 60: إنشاء نقطة فحص ومراجعة الجدول الزمني
- **المواقع**:
  `src/Buy2.Application/Features/Schedules/ValidateDraft/ValidateScheduleDraftCommand.cs`
  `src/Buy2.Api/Controllers/ScheduleValidationController.cs`
- **فلسفة العمل والمنطق الأمني**:
  محرك الفحص الآلي يراجع التداخل والعمل الإضافي وفترات الراحة قبل نشر الجدول.
- **خطوات التنفيذ**:
  1. تعريف أمر الفحص:
     `ValidateScheduleDraftCommand(List<DraftShiftDto> Shifts) : IRequest<PreFlightValidationResultDto>`
  2. في فئة المعالجة:
     - إرسال المناوبات المحتملة لمحرك الفحص
       `IScheduleValidationEngine`
     - إرجاع تقرير التنبيهات والتعارضات.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/schedules/validate-draft`

---

### المهمة 61: إنشاء نقطة سوق المناوبات المتاحة
- **المواقع**:
  `src/Buy2.Application/Features/ShiftMarket/GetOpenShifts/GetOpenShiftsQuery.cs`
  `src/Buy2.Api/Controllers/OpenShiftsController.cs`
- **فلسفة العمل والمنطق الأمني**:
  تمكين الموظفين من استعراض المناوبات غير المكتملة وحجزها.
- **خطوات التنفيذ**:
  1. تعريف استعلام المناوبات المفتوحة:
     `GetOpenShiftsQuery() : IRequest<List<ShiftDto>>`
  2. في فئة المعالجة:
     - الاستعلام عن المناوبات التي بدون موظف وتاريخها في المستقبل.
  3. في المتحكم:
     إضافة نقطة النهاية
     `GET api/v1/shift-market/open-shifts`

---

### المهمة 62: إنشاء نقطة طلب حجز مناوبة
- **المواقع**:
  `src/Buy2.Application/Features/ShiftMarket/ClaimShift/ClaimShiftCommand.cs`
  `src/Buy2.Api/Controllers/ShiftClaimsController.cs`
- **فلسفة العمل والمنطق الأمني**:
  تسجيل طلب الموظف لحجز المناوبة مع مبرر العمل الإضافي لمراجعة المدير.
- **خطوات التنفيذ**:
  1. تعريف أمر الحجز:
     `ClaimShiftCommand(int ShiftId, int EmployeeId, string OvertimeJustification) : IRequest<bool>`
  2. في فئة المعالجة:
     - التأكد من أن المناوبة ما زالت شاغرة.
     - إنشاء طلب جديد بحالة معلقة
       `Pending`
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/shift-market/claims/{id}`

---

### المهمة 63: إنشاء نقطة إضافة قواعد النقاط والمكافآت
- **المواقع**:
  `src/Buy2.Application/Features/Points/CreateRule/CreatePointsRuleCommand.cs`
  `src/Buy2.Api/Controllers/PointsRulesController.cs`
- **فلسفة العمل والمنطق الأمني**:
  نظام التحفيز يمنح نقاطاً آلياً عند الحضور الملتزم أو تغطية المناوبات.
- **خطوات التنفيذ**:
  1. تعريف أمر القاعدة:
     `CreatePointsRuleCommand(string RuleName, int PointsValue, string TriggerType) : IRequest<int>`
  2. في فئة المعالجة:
     - حفظ القاعدة وقيمة النقاط ورابط المحفز.
  3. في المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/points/rules`

---

### المهمة 64: إنشاء نقطة استبدال النقاط بمكافأة
- **المواقع**:
  `src/Buy2.Application/Features/Rewards/RedeemReward/RedeemRewardCommand.cs`
  `src/Buy2.Api/Controllers/RewardRedemptionController.cs`
- **فلسفة العمل والمنطق الأمني**:
  خصم نقاط الموظف وإصدار كود قسيمة الشراء الرقمية من المخزون المتاح.
- **خطوات التنفيذ**:
  1. تعريف أمر الاستبدال:
     `RedeemRewardCommand(int RewardItemId, int EmployeeId) : IRequest<string>`
  2. في فئة المعالجة:
     - فحص رصيد نقاط الموظف للتأكد من كفايته.
     - كتم حجز كود القسيمة من المخزون لمنع التكرار.
     - خصم النقاط وإصدار سجل العملية ثم إرجاع كود القسيمة.
  3. In المتحكم:
     إضافة نقطة النهاية
     `POST api/v1/rewards/{id}/redeem`
