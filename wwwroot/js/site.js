// AI Quiz - Modern Interactive Features

document.addEventListener('DOMContentLoaded', function () {
    // Navbar scroll effect
    const navbar = document.querySelector('.navbar');
    if (navbar) {
        window.addEventListener('scroll', function () {
            if (window.scrollY > 20) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }

    // Option selection for quiz attempt
    document.querySelectorAll('.option-item').forEach(function (item) {
        item.addEventListener('click', function () {
            const radio = this.querySelector('input[type="radio"]');
            if (radio) {
                // Remove selected from siblings
                const name = radio.getAttribute('name');
                document.querySelectorAll('input[name="' + name + '"]').forEach(function (r) {
                    const parent = r.closest('.option-item');
                    if (parent) parent.classList.remove('selected');
                });
                radio.checked = true;
                this.classList.add('selected');
            }
        });
    });

    // Pre-select already checked radios
    document.querySelectorAll('.option-item input[type="radio"]:checked').forEach(function (radio) {
        const item = radio.closest('.option-item');
        if (item) item.classList.add('selected');
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            var target = document.querySelector(this.getAttribute('href'));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });

    // Animate elements on scroll
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.card, .stat-card, .quiz-card').forEach(function (el) {
        el.style.opacity = '0';
        el.style.transform = 'translateY(20px)';
        el.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
        observer.observe(el);
    });

    // Auto-dismiss alerts after 5 seconds
    document.querySelectorAll('.alert').forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-10px)';
            setTimeout(function () {
                alert.remove();
            }, 300);
        }, 5000);
    });

    // Form input focus animations
    document.querySelectorAll('.form-control, .form-select').forEach(function (input) {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'scale(1.01)';
            this.parentElement.style.transition = 'transform 0.2s ease';
        });
        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'scale(1)';
        });
    });
});
