
import { DefaultFooter } from '@ant-design/pro-components';
import React from 'react';
import './index.css';

const Footer: React.FC = () => {
  return (
    <DefaultFooter
      style={{
        background: 'none',
        color: '#f0f0f0',
      }}
      className='Footer'
      copyright={
        <React.Fragment>
          {new Date().getFullYear()} <a href="https://www.zhishuneng.com" target="_blank" rel="noopener noreferrer">知数能信息科技</a>
          <div><a href="https://beian.miit.gov.cn" target="_blank" rel="noopener noreferrer">闽ICP备2024055883号</a></div>
          <div>UI By:<a href="https://pro.ant.design" target="_blank" rel="noopener noreferrer">Ant Design Pro</a></div>
        </React.Fragment>
      }
      links={[{
        title: '出品：福州知数能信息科技有限责任公司',
        href: 'https://www.zhishuneng.com',
        blankTarget: true
      },{
        title: '产品及服务',
        href: '/welcome',
        blankTarget: true
      },{
        title: '隐私政策',
        href: '/PrivatePolicy/Index',
        blankTarget: true
      }
      ]}
    />
  );
};

export default Footer;
