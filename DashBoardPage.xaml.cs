using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace CPK_Calculate
{
    /// <summary>
    /// หน้า Dashboard หลักสำหรับแสดงภาพรวมของระบบและประวัติการคำนวณล่าสุด
    /// </summary>
    public sealed partial class DashBoardPage : Page
    {
        public DashBoardPage()
        {
            this.InitializeComponent();

            // โหลดข้อมูลตัวอย่างแสดงในหน้า Dashboard
            LoadDashboardContent();
        }

        /// <summary>
        /// เตรียมข้อมูลจำลองสำหรับแสดงผลใน RecentHistoryList
        /// </summary>
        private void LoadDashboardContent()
        {
            // เรียกใช้ HistoryItem จากไฟล์ CPKService.cs ได้โดยตรง
            // หมายเหตุ: ต้องมั่นใจว่าในไฟล์ CPKService.cs คลาส HistoryItem ถูกประกาศเป็น 'public'
            var historyData = new List<HistoryItem>
            {
                new HistoryItem { PartName = "Part-A102 (Shaft)", Date = "24-Feb-2026 14:30", Result = "CPK: 1.52" },
                new HistoryItem { PartName = "Part-B990 (Bearing)", Date = "24-Feb-2026 10:15", Result = "CPK: 1.38" },
                new HistoryItem { PartName = "Part-C441 (Housing)", Date = "23-Feb-2026 16:45", Result = "CPK: 1.60" },
                new HistoryItem { PartName = "Part-A105 (Shaft)", Date = "22-Feb-2026 09:20", Result = "CPK: 1.41" },
                new HistoryItem { PartName = "Part-D202 (Gear)", Date = "21-Feb-2026 11:10", Result = "CPK: 1.75" }
            };

            // กำหนดแหล่งข้อมูลให้กับ ListView (RecentHistoryList คือ x:Name ในไฟล์ XAML)
            if (RecentHistoryList != null)
            {
                RecentHistoryList.ItemsSource = historyData;
            }
        }
    }
}