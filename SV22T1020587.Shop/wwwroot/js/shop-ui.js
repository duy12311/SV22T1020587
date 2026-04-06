document.addEventListener('DOMContentLoaded', function () {
    // 1. DOM Elements cho Badge giỏ hàng
    // Hãy đảm bảo ID này khớp với ID của thẻ span/div hiển thị số lượng trên Layout của bạn
    const cartCountElements = document.querySelectorAll('#cartCount, .cart-badge');

    // 2. Hàm gọi lên server lấy tổng số lượng thực tế
    async function syncCartCount() {
        try {
            const response = await fetch('/Cart/Count', { method: 'GET', cache: 'no-cache' });

            if (response.status === 401) {
                // not authenticated -> redirect to login (no badge shown)
                // optional: keep badge hidden
                cartCountElements.forEach(el => el.style.display = 'none');
                return;
            }

            if (response.ok) {
                const data = await response.json();
                const displayCount = parseInt(data.count, 10) || 0;

                cartCountElements.forEach(el => {
                    el.textContent = displayCount;
                    // Ẩn badge nếu không có sản phẩm nào (tùy chọn)
                    el.style.display = displayCount > 0 ? 'inline-block' : 'none';
                });
            }
        } catch (err) {
            console.error('Lỗi đồng bộ giỏ hàng:', err);
        }
    }

    // 3. Chạy đồng bộ ngay khi load mọi trang
    syncCartCount();

    // 4. Expose hàm ra window để các View khác (như file Index giỏ hàng vừa rồi) có thể gọi
    window.refreshCartBadge = syncCartCount;

    // 5. Cấu hình hàm "Thêm vào giỏ hàng" dùng cho trang Danh sách sản phẩm (Product List/Home)
    window.addToCart = async function (productID, productName, salePrice, photo, quantity = 1) {
        try {
            // Khởi tạo object dữ liệu khớp với Model CartItem
            const formData = new URLSearchParams();
            formData.append('ProductID', productID);
            formData.append('ProductName', productName);
            formData.append('SalePrice', salePrice);
            formData.append('Photo', photo);
            formData.append('Quantity', quantity);

            const response = await fetch('/Cart/Add', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: formData.toString()
            });

            if (response.status === 401) {
                // not authenticated -> redirect to login with returnUrl
                const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
                window.location.href = '/Account/Login?returnUrl=' + returnUrl;
                return;
            }

            const result = await response.json();

            if (result.success) {
                window.refreshCartBadge(); // Cập nhật lại số badge ngay lập tức

                // Tùy chọn: Hiện thông báo Toast/Alert nhỏ ở góc màn hình báo thêm thành công
                alert(`Đã thêm ${productName} vào giỏ hàng!`);
            } else {
                alert("Không thể thêm vào giỏ hàng lúc này.");
            }
        } catch (err) {
            console.error("Lỗi khi thêm sản phẩm:", err);
            alert("Lỗi kết nối. Vui lòng thử lại!");
        }
    };
});