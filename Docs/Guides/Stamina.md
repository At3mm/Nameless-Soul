# Stamina

Stamina là tài nguyên sức bền dùng cho chạy nhanh, dash và kỹ năng. Nó độc lập với các chỉ số cơ bản sẽ bổ sung sau.

- `CharacterStats.maxStamina`, `currentStamina`: sức bền tối đa và hiện tại.
- `CharacterStats.StaminaCostMultiplier`: hệ số tiêu hao tổng hợp từ buff và debuff.
- `SkillData.staminaCost`: chi phí dùng kỹ năng.
- `PlayerDash.dashStaminaCost`: chi phí dash.
- `PlayerController.sprintStaminaPerSecond`: tiêu hao khi chạy nhanh.
- `staminaRecoveryPerSecond`, `staminaRecoveryDelay`, `DelayStaminaRecovery()`: hồi sức bền và thời gian chờ.
- `CharacterBuffs.Kind.StaminaEfficiency`: giảm tiêu hao, giữ giá trị enum 3 để bảo toàn dữ liệu đã lưu.
- `CharacterDebuffs.stressStaminaCostMultiplier`: mức tăng tiêu hao khi căng thẳng.
- `PlayerStun.exhaustedStaminaPercent`: ngưỡng cạn sức để có thể bị choáng.
- `PlayerVitalsHUD.staminaFill`, `staminaValue`, `staminaLossFill`: thanh sức bền và hiệu ứng mất sức bền. Tên Vitals của lớp chỉ nhóm các chỉ báo sinh tồn chung, không phải một chỉ số cơ bản.

Toàn bộ khóa trường liên quan trong prefab và cấu hình HUD đã được chuyển trực tiếp cùng code, giữ nguyên giá trị, tham chiếu và GUID. Không giữ thuộc tính chuyển tiếp với tên sức bền cũ để tránh xung đột khi thêm chỉ số mới. Bản sao trước chuyển đổi nằm trong `Library/StaminaRenameBackup`.

Trong Play Mode, chạy **Tools → Diagnostics → Test Stamina** để kiểm tra chi phí kỹ năng/dash, hệ số buff và stress, hồi phục và giới hạn tối đa. **Test Exhaustion Stun** kiểm tra thêm hành vi choáng khi cạn sức.
