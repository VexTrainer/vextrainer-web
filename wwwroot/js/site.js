// ===== VexTrainer Web - Complete JavaScript =====
// Single JS file for entire site

// Initialize VexTrainer namespace immediately
window.VexTrainer = window.VexTrainer || {};

// ===== User Menu Toggle =====
function toggleUserMenu() {
    const dropdown = document.getElementById('userDropdown');
    dropdown.classList.toggle('show');
}

// Close dropdown when clicking outside
document.addEventListener('click', function(event) {
    const userMenu = document.querySelector('.user-menu');
    if (userMenu && !userMenu.contains(event.target)) {
        const dropdown = document.getElementById('userDropdown');
        if (dropdown) {
            dropdown.classList.remove('show');
        }
    }
});

// ===== Password Toggle =====
function togglePassword(inputId, iconId) {
    const input = document.getElementById(inputId ||'password');
    const icon = document.getElementById(iconId || 'eyeIcon');
    
    if (input.type === 'password') {
        input.type = 'text';
        if (icon) icon.textContent = '🔒';
    } else {
        input.type = 'password';
        if (icon) icon.textContent = '👁️';
    }
}

// ===== Form Validation Helpers =====
function showError(fieldId, message) {
    const errorElement = document.getElementById(fieldId);
    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    }
}

function clearError(fieldId) {
    const errorElement = document.getElementById(fieldId);
    if (errorElement) {
        errorElement.textContent = '';
        errorElement.style.display = 'none';
    }
}

function clearAllErrors() {
    const errors = document.querySelectorAll('.error-message');
    errors.forEach(error => {
        error.textContent = '';
        error.style.display = 'none';
    });
}

function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validatePassword(password) {
    // At least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special char
    if (password.length < 8) return false;
    if (!/[A-Z]/.test(password)) return false;
    if (!/[a-z]/.test(password)) return false;
    if (!/[0-9]/.test(password)) return false;
    if (!/[!@#$%^&*]/.test(password)) return false;
    return true;
}

// ===== Message Display =====
function showMessage(message, type = 'info') {
    const messageDiv = document.createElement('div');
    messageDiv.className = `alert alert-${type}`;
    messageDiv.innerHTML = `
        ${message}
        <button type="button" class="alert-close" onclick="this.parentElement.remove()">×</button>
    `;
    
    const main = document.querySelector('.main-content');
    if (main) {
        main.insertBefore(messageDiv, main.firstChild);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            messageDiv.remove();
        }, 5000);
    }
}

// ===== AJAX Helper =====
async function fetchJSON(url, options = {}) {
    try {
        const response = await fetch(url, {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        });
        
        const data = await response.json();
        return { success: response.ok, data, status: response.status };
    } catch (error) {
        return { success: false, error: error.message };
    }
}

// ===== Progress Bar Animation =====
function animateProgress(elementId, targetPercent) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    let current = 0;
    const step = targetPercent / 50; // 50 steps
    
    const timer = setInterval(() => {
        current += step;
        if (current >= targetPercent) {
            current = targetPercent;
            clearInterval(timer);
        }
        element.style.width = current + '%';
    }, 20);
}

// ===== Auto-dismiss Alerts =====
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });
});

// ===== Smooth Scroll =====
function smoothScrollTo(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}

// ===== Confirm Action =====
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// ===== Copy to Clipboard =====
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showMessage('Copied to clipboard!', 'success');
    } catch (err) {
        showMessage('Failed to copy', 'error');
    }
}

// ===== Form Utilities =====
function serializeForm(formElement) {
    const formData = new FormData(formElement);
    const data = {};
    for (let [key, value] of formData.entries()) {
        data[key] = value;
    }
    return data;
}

// ===== Loading Spinner =====
function showLoading(buttonElement) {
    if (buttonElement) {
        buttonElement.disabled = true;
        buttonElement.dataset.originalText = buttonElement.textContent;
        buttonElement.textContent = 'Loading...';
    }
}

function hideLoading(buttonElement) {
    if (buttonElement) {
        buttonElement.disabled = false;
        buttonElement.textContent = buttonElement.dataset.originalText || 'Submit';
    }
}

// ===== Debounce Function =====
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ===== Export functions for use in inline scripts =====
// Extend VexTrainer namespace with utility functions
Object.assign(window.VexTrainer, {
    toggleUserMenu,
    togglePassword,
    showError,
    clearError,
    clearAllErrors,
    validateEmail,
    validatePassword,
    showMessage,
    fetchJSON,
    animateProgress,
    smoothScrollTo,
    confirmAction,
    copyToClipboard,
    serializeForm,
    showLoading,
    hideLoading,
    debounce,
    
    
    // SignIn page functionality
    SignIn: {
        init: function() {
            const form = document.getElementById('signinForm');
            if (form) {
                form.addEventListener('submit', function(e) {
                    const password = document.getElementById('password');
                    if (password && password.value.length < 8) {
                        e.preventDefault();
                        showError('passwordError', 'Password must be at least 8 characters');
                        return false;
                    }
                    clearError('passwordError');
                    return true;
                });
            }
        },
        
        handleSignIn: function() {
            const email = document.getElementById('email').value;
            
            if (!email) {
                showError('emailError', 'Email is required');
                return;
            }
            
            if (!validateEmail(email)) {
                showError('emailError', 'Please enter a valid email address');
                return;
            }
            
            clearError('emailError');
            
            // Show password field, hide other buttons
            document.getElementById('passwordGroup').style.display = 'block';
            document.getElementById('buttonRow').style.display = 'none';
            document.getElementById('submitRow').style.display = 'block';
            document.getElementById('password').focus();
        },
        
        handleForgotPassword: async function() {
            const email = document.getElementById('email').value;
            
            if (!email) {
                showError('emailError', 'Email is required');
                return;
            }
            
            if (!validateEmail(email)) {
                showError('emailError', 'Please enter a valid email address');
                return;
            }
            
            clearError('emailError');
            
            // Show loading
            const messageDiv = document.getElementById('responseMessage');
            messageDiv.textContent = 'Sending password reset link...';
            messageDiv.className = 'message info';
            messageDiv.style.display = 'block';
            
            // AJAX call to forgot password endpoint
            // Get token from page
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenInput ? tokenInput.value : '';
            try {
                const response = await fetch('/Auth/ForgotPassword', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token 
                    },
                    body: JSON.stringify({ email: email })
                });
                
                const result = await response.json();
                
                if (result.success) {
                    messageDiv.textContent = 'Password reset link sent to your email! Please check your inbox.';
                    messageDiv.className = 'message success';
                } else {
                    messageDiv.textContent = result.message || 'Error sending reset link. Please try again.';
                    messageDiv.className = 'message error';
                }
            } catch (error) {
                messageDiv.textContent = 'Network error. Please try again.';
                messageDiv.className = 'message error';
            }
        },
        
        handleSignUp: function() {
            const email = document.getElementById('email').value;
            
            if (!email) {
                showError('emailError', 'Email is required');
                return;
            }
            
            if (!validateEmail(email)) {
                showError('emailError', 'Please enter a valid email address');
                return;
            }
            
            // Redirect to register page with email pre-filled
            window.location.href = '/Auth/Register?email=' + encodeURIComponent(email);
        },
        
        resetForm: function() {
            document.getElementById('passwordGroup').style.display = 'none';
            document.getElementById('buttonRow').style.display = 'flex';
            document.getElementById('submitRow').style.display = 'none';
            document.getElementById('password').value = '';
            document.getElementById('responseMessage').style.display = 'none';
            clearAllErrors();
        }
    },
    
    // Register page functionality
    Register: {
        init: function() {
            const form = document.getElementById('registerForm');
            if (!form) return;
            
            // Real-time password validation
            const password = document.getElementById('password');
            const confirmPassword = document.getElementById('confirmPassword');
            
            if (password) {
                password.addEventListener('input', function() {
                    VexTrainer.Register.validatePasswordStrength();
                });
            }
            
            if (confirmPassword) {
                confirmPassword.addEventListener('input', function() {
                    VexTrainer.Register.validatePasswordMatch();
                });
            }
            
            // Form submission
            form.addEventListener('submit', function(e) {
                if (!VexTrainer.Register.validateForm()) {
                    e.preventDefault();
                    return false;
                }
            });
        },
        
        validatePasswordStrength: function() {
            const password = document.getElementById('password').value;
            const errorElement = document.getElementById('passwordError');
            
            if (!password) {
                clearError('passwordError');
                return true;
            }
            
            if (password.length < 8) {
                showError('passwordError', 'Password must be at least 8 characters');
                return false;
            }
            
            if (!validatePassword(password)) {
                showError('passwordError', 'Password must include uppercase, lowercase, number, and special character (!@#$%^&*)');
                return false;
            }
            
            clearError('passwordError');
            return true;
        },
        
        validatePasswordMatch: function() {
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            
            if (!confirmPassword) {
                clearError('confirmPasswordError');
                return true;
            }
            
            if (password !== confirmPassword) {
                showError('confirmPasswordError', 'Passwords do not match');
                return false;
            }
            
            clearError('confirmPasswordError');
            return true;
        },
        
        validateForm: function() {
            let isValid = true;
            
            // Validate email
            const email = document.getElementById('email').value;
            if (!email) {
                showError('emailError', 'Email is required');
                isValid = false;
            } else if (!validateEmail(email)) {
                showError('emailError', 'Please enter a valid email address');
                isValid = false;
            } else {
                clearError('emailError');
            }
            
            // Validate password strength
            if (!VexTrainer.Register.validatePasswordStrength()) {
                isValid = false;
            }
            
            // Validate password match
            if (!VexTrainer.Register.validatePasswordMatch()) {
                isValid = false;
            }
            
            return isValid;
        }
    }
});

// ===== Accordion Toggle Function =====
function toggleAccordion(id) {
    const content = document.getElementById(id);
    const icon = document.getElementById(id + '-icon');
    
    if (content && icon) {
        if (content.style.display === 'none' || content.style.display === '') {
            content.style.display = 'block';
            icon.classList.add('expanded');
        } else {
            content.style.display = 'none';
            icon.classList.remove('expanded');
        }
    }
}

// ===== Lesson Content Loader Module =====
// Handles markdown loading, syntax highlighting, and topic navigation
VexTrainer.LessonContent = {
    // Configuration
    config: {
        fileName: null,
        topicId: null,
        hasNextTopic: false,
        nextTopicUrl: null
    },
    
    // Initialize the lesson content loader
    init: function(fileName, topicId, hasNextTopic, nextTopicUrl) {
        this.config.fileName = fileName;
        this.config.topicId = topicId;
        this.config.hasNextTopic = hasNextTopic;
        this.config.nextTopicUrl = nextTopicUrl;
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.onDOMReady());
        } else {
            this.onDOMReady();
        }
    },
    
    // Called when DOM is ready
    onDOMReady: function() {
        console.log('Lesson Content: DOM loaded');
        this.loadContent();
        this.initMarkCompleteButton();
        this.initKeyboardNavigation();
    },
    
    // Load and render markdown content
    loadContent: async function() {
        console.log('Lesson Content: Starting content fetch...');
        
        const contentElement = document.getElementById('lessonContent');
        if (!contentElement) {
            console.error('ERROR: lessonContent element not found in DOM!');
            return;
        }
        
        const fileName = this.config.fileName;
        const topicId = this.config.topicId;
        
        console.log('FileName:', fileName);
        console.log('TopicId:', topicId);
        
        try {
            // Fetch markdown content
            console.log('Fetching content from handler...');
            const response = await fetch('?handler=Content');
            
            console.log('Response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Response not OK:', errorText);
                throw new Error('Topic content not found');
            }
            
            const markdown = await response.text();
            console.log('Markdown loaded, length:', markdown.length);
            
            // Parse markdown to HTML
            if (typeof marked === 'undefined') {
                throw new Error('marked.js library not loaded');
            }
            
            const html = marked.parse(markdown);
            console.log('HTML parsed, length:', html.length);
            
            // Display content
            contentElement.innerHTML = html;
            console.log('Content rendered successfully');
            
            // Apply syntax highlighting to code blocks
            this.applySyntaxHighlighting();
            
        } catch (error) {
            console.error('Error loading topic:', error);
            contentElement.innerHTML = 
                '<div class="error">Error loading topic. Please try again.</div>';
        }
    },
    
    // Apply Prism.js syntax highlighting with CUSTOM line numbers and copy button
    applySyntaxHighlighting: function() {
        if (typeof Prism === 'undefined') {
            console.warn('Prism.js not loaded, skipping syntax highlighting');
            return;
        }
        
        document.querySelectorAll('pre code').forEach((block) => {
            const pre = block.parentElement;
            if (!pre) return;
            
            // Skip if already processed
            if (pre.classList.contains('code-processed')) return;
            
            // Detect language from markdown or default to cpp
            let language = 'cpp';
            const existingLangMatch = block.className.match(/language-(\w+)/);
            if (existingLangMatch) {
                language = existingLangMatch[1];
            }
            
            // Set language class
            block.className = `language-${language}`;
            
            // Apply Prism highlighting
            try {
                Prism.highlightElement(block);
            } catch (err) {
                console.warn('Prism highlighting failed:', err);
            }
            
            // Get the code text
            const codeText = block.textContent;
            const lines = codeText.split('\n');
            
            // Remove empty last line if exists
            if (lines[lines.length - 1].trim() === '') {
                lines.pop();
            }
            
            // Build HTML with line numbers
            let numberedHtml = '<div class="code-container">';
            numberedHtml += '<div class="line-numbers-wrapper">';
            
            // Add line numbers
            for (let i = 1; i <= lines.length; i++) {
                numberedHtml += `<span class="line-number">${i}</span>\n`;
            }
            numberedHtml += '</div>';
            
            // Add code content (already highlighted by Prism)
            numberedHtml += '<div class="code-content">';
            numberedHtml += block.innerHTML;
            numberedHtml += '</div>';
            
            // Add copy button
            numberedHtml += `<button class="copy-code-btn" onclick="VexTrainer.LessonContent.copyCode(this)" title="Copy code">Copy</button>`;
            
            numberedHtml += '</div>';
            
            // Replace pre content
            pre.innerHTML = numberedHtml;
            pre.classList.add('code-processed');
        });
        
        console.log('Syntax highlighting applied with custom line numbers and copy button');
    },
    
    // Copy code to clipboard
    copyCode: function(button) {
        const codeContainer = button.closest('.code-container');
        const codeContent = codeContainer.querySelector('.code-content');
        const text = codeContent.textContent;
        
        navigator.clipboard.writeText(text).then(() => {
            const originalText = button.textContent;
            button.textContent = '✓ Copied!';
            button.classList.add('copied');
            
            setTimeout(() => {
                button.textContent = originalText;
                button.classList.remove('copied');
            }, 2000);
        }).catch(err => {
            console.error('Failed to copy:', err);
            button.textContent = 'Failed';
            setTimeout(() => {
                button.textContent = 'Copy';
            }, 2000);
        });
    },
    
    // Initialize "Mark as Complete" button
    initMarkCompleteButton: function() {
        const markCompleteBtn = document.getElementById('markCompleteBtn');
        if (!markCompleteBtn) {
            console.warn('Mark Complete button not found');
            return;
        }
        
        markCompleteBtn.addEventListener('click', async (e) => {
            await this.handleMarkComplete(e.target);
        });
    },
    
    // Handle mark as complete action
    handleMarkComplete: async function(btn) {
        const originalText = btn.textContent;
        
        try {
            btn.disabled = true;
            btn.textContent = 'Marking...';
            
            // Get antiforgery token
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenInput) {
                console.error('Antiforgery token not found');
                throw new Error('Security token not found');
            }
            const token = tokenInput.value;
            
            console.log('Marking topic as read:', this.config.topicId);
            
            // Send request to mark topic as read
            const response = await fetch('?handler=MarkRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ topicId: this.config.topicId })
            });
            
            console.log('Mark complete response status:', response.status);
            
            // Check if response is OK
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Mark complete failed:', response.status, errorText);
                throw new Error(`Server error: ${response.status}`);
            }
            
            const result = await response.json();
            console.log('Mark complete result:', result);
            
            if (result.success) {
                btn.textContent = '✓ Completed';
                btn.classList.remove('btn-success');
                btn.classList.add('btn-secondary');
                
                // Redirect to next topic if available
                if (this.config.hasNextTopic && this.config.nextTopicUrl) {
                    setTimeout(() => {
                        window.location.href = this.config.nextTopicUrl;
                    }, 1000);
                }
            } else {
                btn.textContent = originalText;
                btn.disabled = false;
                alert('Error marking topic as complete: ' + (result.message || 'Unknown error'));
            }
            
        } catch (error) {
            console.error('Error marking topic as complete:', error);
            btn.textContent = originalText;
            btn.disabled = false;
            alert('Error marking topic as complete. Please try again.');
        }
    },
    
    // Initialize keyboard navigation (arrow keys)
    initKeyboardNavigation: function() {
        // Store references for navigation
        const prevUrl = this.getPrevTopicUrl();
        const nextUrl = this.getNextTopicUrl();
        
        document.addEventListener('keydown', (e) => {
            // Don't trigger if modifier keys are pressed or if user is typing
            if (e.ctrlKey || e.metaKey || e.altKey || this.isTyping()) {
                return;
            }
            
            if (e.key === 'ArrowLeft' && prevUrl) {
                window.location.href = prevUrl;
            } else if (e.key === 'ArrowRight' && nextUrl) {
                window.location.href = nextUrl;
            }
        });
    },
    
    // Get previous topic URL from the page
    getPrevTopicUrl: function() {
        const prevBtn = document.querySelector('.nav-btn.prev');
        return prevBtn ? prevBtn.getAttribute('href') : null;
    },
    
    // Get next topic URL from the page
    getNextTopicUrl: function() {
        const nextBtn = document.querySelector('.nav-btn.next');
        return nextBtn ? nextBtn.getAttribute('href') : null;
    },
    
    // Check if user is currently typing in an input field
    isTyping: function() {
        const activeElement = document.activeElement;
        return activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.isContentEditable
        );
    }
};
