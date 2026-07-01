layui.use(function () {
    var $ = layui.$;
    var layer = layui.layer;

    var currentStep = 1;
    var envChecked = false;
    var envOk = false;

    function showStep(step) {
        $('.step-item').removeClass('active');
        $('.step-content').removeClass('active');

        $('.step-item[data-step="' + step + '"]').addClass('active');
        $('.step-content[data-step="' + step + '"]').addClass('active');

        // 标记之前步骤为完成
        for (var i = 1; i < step; i++) {
            $('.step-item[data-step="' + i + '"]').addClass('completed');
            $('.step-item[data-step="' + i + '"]').find('.step-number').html('<i class="layui-icon layui-icon-ok"></i>');
        }

        updateButtons(step);
    }

    function updateButtons(step) {
        if (step === 1) {
            $('#btn-prev').hide();
            $('#btn-next').show();
            $('#btn-submit').hide();
        } else if (step === 2) {
            $('#btn-prev').show();
            $('#btn-next').show();
            $('#btn-submit').hide();
            $('#btn-next').text('下一步');
            // 环境检测失败时仍允许继续，仅移除禁用样式
            $('#btn-next').prop('disabled', false).removeClass('layui-btn-disabled');
        } else if (step === 3) {
            $('#btn-prev').show();
            $('#btn-next').hide();
            $('#btn-submit').show();
            $('#btn-submit').removeClass('layui-btn-disabled').prop('disabled', false);
        }
    }

    function checkEnvironment() {
        envChecked = true;
        $('.check-item').removeClass('success error');
        $('.check-icon').each(function () {
            $(this).html('<i class="layui-icon layui-icon-loading layui-anim layui-anim-rotate layui-anim-loop"></i>');
        });
        $('.check-status').text('检测中...');
        $('.check-result-msg').removeClass('error success').hide().text('');
        $('#btn-next').addClass('layui-btn-disabled').prop('disabled', true);

        $.ajax({
            url: '/Manage/Welcome/CheckEnvironment',
            type: 'POST',
            dataType: 'json',
            success: function (result) {
                envOk = result.allOk;

                updateCheckItem('database', result.databaseOk, result.databaseMessage);
                updateCheckItem('api', result.apiOk, result.apiMessage);
                updateCheckItem('redis', result.redisOk, result.redisMessage);

                if (result.allOk) {
                    $('.check-result-msg').addClass('success').text('环境检测通过，点击"下一步"继续。').show();
                } else {
                    var msg = '部分环境检测未通过，您仍可点击"下一步"继续，但部分功能可能无法使用。';
                    $('.check-result-msg').addClass('error').text(msg).show();
                }

                updateButtons(2);
            },
            error: function (xhr) {
                envOk = false;
                $('.check-item').addClass('error');
                $('.check-status').text('检测失败');
                $('.check-icon').html('<i class="layui-icon layui-icon-close"></i>');
                $('.check-result-msg').addClass('error').text('请求失败: ' + (xhr.responseText || xhr.statusText)).show();
            }
        });
    }

    function updateCheckItem(name, ok, message) {
        var $item = $('.check-item[data-check="' + name + '"]');
        var $icon = $item.find('.check-icon');
        var $status = $item.find('.check-status');

        if (ok) {
            $item.addClass('success');
            $icon.html('<i class="layui-icon layui-icon-ok"></i>');
            $status.text('正常');
        } else {
            $item.addClass('error');
            $icon.html('<i class="layui-icon layui-icon-close"></i>');
            $status.text(message || '异常');
        }
    }

    function submitStartInfo() {
        var consent = $('input[name="consent"]:checked').val() === 'true';
        var btnText = $('#btn-submit').text();
        $('#btn-submit').addClass('layui-btn-disabled').prop('disabled', true).text('提交中...');

        $.ajax({
            url: '/Manage/Welcome/SubmitStartInfo',
            type: 'POST',
            data: { consent: consent },
            dataType: 'json',
            success: function (result) {
                if (result && result.success) {
                    showStep(4);
                    $('#btn-prev').hide();
                    $('#btn-next').hide();
                    $('#btn-submit').hide();
                    setTimeout(function () {
                        window.location.href = '/Manage';
                    }, 1500);
                } else {
                    layer.msg(result.message || '提交失败', { icon: 2 });
                    $('#btn-submit').removeClass('layui-btn-disabled').prop('disabled', false).text(btnText);
                }
            },
            error: function (xhr) {
                layer.msg('请求失败: ' + (xhr.responseText || xhr.statusText), { icon: 2 });
                $('#btn-submit').removeClass('layui-btn-disabled').prop('disabled', false).text(btnText);
            }
        });
    }

    $('#btn-next').on('click', function () {
        if ($(this).hasClass('layui-btn-disabled')) return;

        if (currentStep === 1) {
            currentStep = 2;
            showStep(2);
            if (!envChecked) {
                checkEnvironment();
            }
        } else if (currentStep === 2) {
            if (!envOk) {
                layer.msg('环境检测未通过，您仍可继续，但部分功能可能无法使用', { icon: 0 });
            }
            currentStep = 3;
            showStep(3);
        }
    });

    $('#btn-prev').on('click', function () {
        if (currentStep === 2) {
            currentStep = 1;
            showStep(1);
        } else if (currentStep === 3) {
            currentStep = 2;
            showStep(2);
        }
    });

    $('#btn-submit').on('click', function () {
        if ($(this).hasClass('layui-btn-disabled')) return;
        submitStartInfo();
    });

    // 初始化
    showStep(1);
});
