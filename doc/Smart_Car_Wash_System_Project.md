# Smart Automated Car Wash Management System - Project Overview

## Topic: TV:Hệ thống quản lý rửa tự động thông minh với đặt lịch trước và chương trình khách hàng thân thiết
TA:Smart Automated Car Wash Management System with Advance Booking & Loyalty Program

**Code:** SU26SWP01  
**Submitted by:** VanTTN2  
**Responsible by:** nan

### Context
With over 7.7 million vehicles(Motobikes) in Vietnam and 25% YoY growth in car wash demand, customer retention is key. Studies show loyal customers spend 67% more and visit 3x more often. Current systems lack:
• Personalized rewards beyond “wash 5 get 1 free”
• Tiered benefits (Silver/Gold/Platinum)
• Digital tracking of points, history, and perks
AutoWash Pro integrates a multi-tier loyalty program with advance booking, LPR automation, and AI personalization
— boosting repeat rate by 45%, lifetime value by 60%, and enabling data-driven promotions.


### Problems & Solution
AutoWash Pro is a smart, automated car wash management system that integrates AI, and CRM technologies to enhance customer experience, optimize operations, and increase business revenue.
The system supports:
Advanced booking with tier-based priority
AI-powered customer engagement and personalization(optional)
A multi-tier loyalty program to retain and reward customers


### Primary Actors
Target Users:
Customers: owners who want convenient booking and rewards
Admin: moto bikes wash operators managing daily operations, analyze performance and configuring promotions


### Functional Requirements
The system includes core functions:
• Loyalty Engine
o Track points, spend, visits
o Auto-upgrade/down grade (monthly review)
o Redemption: Points → discount, free wash, add-on
o Expiry: Points expire after 12 months
• Customer :
o Linked to license plate+ phone
o View: points balance, wash history
o Tier-based booking window (eg.Member: 7 days, Silver: 10 days, Gold: 12 days, Platinum: 14 days), Priority queue: Higher tier = earlier access
o Auto-apply perks at checkout
• Admin:
o Configure tier rules, point rates, perks, run targeted promos(eg. run a promotion “Send to Silver+ only”) 
(Note: the team cannot implement online payment service and manage refund)



### Research Details (RBL)
**Research Questions:** RQ: What factors most influence customer loyalty tier progression in smart service ecosystems?
Research objectives: to identify the factors that most strongly influence: upgrading tiers, retention, long-term engagement
Thời gian dự kiến: 16 tuần
Cách làm đề xuất:
- SV làm booking website + làm khảo sát người dùng Việt Nam (Survey) + xin dataset từ các chuỗi rửa xe hiện có
=> SV cần tạo 1 biểu mẫu khảo sát , cho chạy thử để thu thập log tại ĐH FPT, các bãi xe, chỗ rửa xe,...
- Log chứa: ngày giờ đặt, số tiền, loyalty points, điểm tích lũy, số lần wash,dùng reward?, loại xe,...
Tiến trình:
Giai đoạn 1(Tuần 1-4) : Xây dựng prototype system
=> làm servey, xây dựng website gồm các chức năng: đăng ký tài khoản, booking 
=> để thu thập data: khách hàng, booking 
=> Đối tượng khảo sát: Sinh viên+ Nhân viên FPT, người dùng app, chủ xe, người dân, nhân viên rửa xe
=> Kênh làm khảo sát: FB, Tiktok, các hội nhóm
Giai đoạn 2(Tuần 5-7) : thu thập synthetic behavioral dataset
=> Hoàn thành module Loyalty, promotion, transaction module
=> thu thập data:tính điểm, upgrade tier, redeem rewards, spending, order value
=> quy mô data: 2k data
Giai đoạn 3(Tuần 8-9): thu thập data từ bên ngoài bằng servey 
=> Mục tiêu thu thập data: thêm 3k data
Giai đoạn 4( Tuần 10-12): Xử lý data
Giai đoạn 5(Tuần 13-16): Generate Synthetic Behavioral Data
=>train ML models, perform analytics, test hypotheses + paper ressult(submit conferrence)


### Project Timeline & Methodology
nan

---

